using UnityEngine;
using GoogleMobileAds.Api;
using System;
using System.Collections;
using System.Collections.Generic;

public class AdManager : MonoBehaviour
{
    public static AdManager Instance;
    private InterstitialAd interstitialAd;
    private RewardedAd rewardedAd;

    bool isAdReady = false;
    bool isLoadingAd = false;
    bool rewardEarned = false;
    Action rewardCallback;

    private System.Action pendingInterstitialCallback;

    // ⚠️ SET TO FALSE BEFORE RELEASE BUILD
    // true  = Google test ads (safe to run, never click real ads on your own device)
    // false = Real ads (only for production APK submitted to Play Store)
    private const bool TEST_MODE = false;

    // Test IDs — Google's official test ad units, always safe during development
    private const string TEST_REWARDED     = "ca-app-pub-3940256099942544/5224354917";
    private const string TEST_INTERSTITIAL = "ca-app-pub-3940256099942544/1033173712";

    // ⚠️ TODO: REPLACE WITH YOUR REAL IDs FROM ADMOB DASHBOARD BEFORE RELEASE
  private const string REAL_REWARDED     = "ca-app-pub-1561072595701997/7692227720";
private const string REAL_INTERSTITIAL = "ca-app-pub-1561072595701997/6570717744";

    private string rewardedAdUnitId     => TEST_MODE ? TEST_REWARDED     : REAL_REWARDED;
    private string interstitialAdUnitId => TEST_MODE ? TEST_INTERSTITIAL : REAL_INTERSTITIAL;

    // Thread-safe queue — ad callbacks post here, Update() drains on main thread
    private readonly Queue<Action> _mainThreadQueue = new Queue<Action>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        // Drain all queued callbacks on the Unity main thread
        while (_mainThreadQueue.Count > 0)
        {
            Action action;
            lock (_mainThreadQueue) { action = _mainThreadQueue.Dequeue(); }
            action?.Invoke();
        }
    }

    // Post any action to run next frame on the main thread
    private void RunOnMainThread(Action action)
    {
        lock (_mainThreadQueue) { _mainThreadQueue.Enqueue(action); }
    }

    void Start()
    {
        Debug.Log("Initializing AdMob...");
        MobileAds.Initialize(initStatus =>
        {
            Debug.Log("✅ AdMob Initialized");
            LoadRewardedAd();
            LoadInterstitial();
        });
    }

    public void LoadRewardedAd()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            Invoke(nameof(LoadRewardedAd), 10f);
            return;
        }
        if (isLoadingAd || isAdReady) return;
        isLoadingAd = true;

        AdRequest request = new AdRequest();
        RewardedAd.Load(rewardedAdUnitId, request, (ad, error) =>
        {
            isLoadingAd = false;
            if (error != null || ad == null)
            {
                Debug.Log("❌ Failed to load rewarded ad: " + error);
                isAdReady = false;
                Invoke(nameof(LoadRewardedAd), 5f);
                return;
            }
            Debug.Log("✅ Rewarded Ad Loaded");
            rewardedAd = ad;
            isAdReady  = true;
            RegisterEvents(rewardedAd);
        });
    }

    void RegisterEvents(RewardedAd ad)
    {
        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("Ad Closed");
            RunOnMainThread(() => StartCoroutine(HandleAdClosed()));
            rewardedAd = null;
            isAdReady  = false;
            RunOnMainThread(LoadRewardedAd);
        };
    }

    IEnumerator HandleAdClosed()
    {
        yield return null;
        if (rewardEarned)
        {
            Debug.Log("Reward earned — executing callback");
            rewardCallback?.Invoke();
            rewardEarned = false;
        }
        else
        {
            Debug.Log("Ad closed without reward — skipped");
            // Do nothing — OnReviveClicked onSkipped handles this case
        }
    }

    public void ShowAd(Action onRewardEarned, Action onSkipped = null)
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            onSkipped?.Invoke();
            return;
        }
        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            isAdReady     = false;
            rewardCallback = onRewardEarned;
            rewardEarned  = false;
            AnalyticsEvents.LogAdImpression("rewarded", "revive");
            rewardedAd.Show((Reward reward) =>
            {
                Debug.Log("✅ Reward earned");
                rewardEarned = true;
            });
        }
        else
        {
            // Ad not ready yet — try to load and wait up to 10s
            Debug.Log("Rewarded ad not ready — loading and waiting");
            LoadRewardedAd();
            StartCoroutine(WaitForRewardedThenShow(onRewardEarned, onSkipped, 10f));
        }
    }

    IEnumerator WaitForRewardedThenShow(Action onRewardEarned, Action onSkipped, float maxWait)
    {
        float elapsed = 0f;
        while (elapsed < maxWait)
        {
            if (rewardedAd != null && rewardedAd.CanShowAd())
            {
                isAdReady      = false;
                rewardCallback = onRewardEarned;
                rewardEarned   = false;
                rewardedAd.Show((Reward reward) =>
                {
                    Debug.Log("✅ Reward earned");
                    rewardEarned = true;
                });
                yield break;
            }
            elapsed += 0.5f;
            yield return new WaitForSecondsRealtime(0.5f);
        }
        // Truly unavailable after waiting — now call onSkipped
        Debug.Log("Rewarded ad unavailable after waiting — skipping");
        onSkipped?.Invoke();
    }

    public void ShowInterstitial(System.Action onComplete)
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            onComplete?.Invoke();
            return;
        }
        if (interstitialAd != null && interstitialAd.CanShowAd())
        {
            AnalyticsEvents.LogAdImpression("interstitial", "game_over");
            pendingInterstitialCallback = onComplete;
            interstitialAd.Show();
        }
        else
        {
            // Ad not ready — try to load then wait up to 3 seconds before fallback
            Debug.Log("Interstitial not ready — attempting load then wait");
            LoadInterstitial();
            StartCoroutine(WaitForInterstitialThenShow(onComplete, 3f));
        }
    }

    IEnumerator WaitForInterstitialThenShow(System.Action onComplete, float maxWait)
    {
        float elapsed = 0f;
        while (elapsed < maxWait)
        {
            if (interstitialAd != null && interstitialAd.CanShowAd())
            {
                pendingInterstitialCallback = onComplete;
                interstitialAd.Show();
                yield break;
            }
            elapsed += 0.25f;
            yield return new WaitForSecondsRealtime(0.25f);
        }
        // Timed out — run game without ad
        Debug.Log("Interstitial load timed out — skipping ad");
        onComplete?.Invoke();
    }

    public void LoadInterstitial()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            Invoke(nameof(LoadInterstitial), 10f);
            return;
        }
        if (interstitialAd != null)
        {
            interstitialAd.Destroy();
            interstitialAd = null;
        }

        AdRequest request = new AdRequest();
        InterstitialAd.Load(interstitialAdUnitId, request, (InterstitialAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
                Debug.Log("❌ Interstitial failed to load: " + error);
                Invoke(nameof(LoadInterstitial), 5f);
                return;
            }
            Debug.Log("✅ Interstitial loaded");
            interstitialAd = ad;

            interstitialAd.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log("Interstitial closed");
                // CRITICAL: fire callback on main thread, not ad thread
                RunOnMainThread(() =>
                {
                    LoadInterstitial(); // preload next
                    var cb = pendingInterstitialCallback;
                    pendingInterstitialCallback = null;
                    cb?.Invoke();
                });
            };
        });
    }
}