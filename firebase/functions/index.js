const { onRequest } = require("firebase-functions/v2/https");
const { setGlobalOptions } = require("firebase-functions/v2");
const admin = require("firebase-admin");

admin.initializeApp();
const db = admin.database();

// Deploy all functions to us-central1 to match the default Firebase region
setGlobalOptions({ region: "us-central1" });

// ── Constants ────────────────────────────────────────────────────────────────

// Flappy Bird-style: ~1 pipe every 2.5 seconds, max realistic session ~20 min
// 20 * 60 / 2.5 = 480 pipes. We allow 999 to not block genuine world records.
const MAX_SCORE = 999;

// Minimum seconds a session must last to be plausible for a given score.
// Each pipe takes ~2.5 seconds minimum. We use 1.5s to be lenient.
const MIN_SECONDS_PER_POINT = 1.5;

// Rate limit: one submission per player per this many seconds.
// Prevents replay attacks — even if the same packet is resent instantly.
const RATE_LIMIT_SECONDS = 10;

// ── submitScore ──────────────────────────────────────────────────────────────
//
// HTTPS endpoint called by the Unity client instead of writing to the DB directly.
// Body (JSON):
//   username       string   — player display name
//   score          int      — final score this run
//   sessionId      string   — UUID generated at run start (prevents replay)
//   sessionSeconds float    — how many real seconds the session lasted
//
// On success returns: { "ok": true }
// On failure returns: { "ok": false, "error": "<reason>" }  with HTTP 400
//
exports.submitScore = onRequest({ cors: false }, async (req, res) => {
  // Only accept POST
  if (req.method !== "POST") {
    return res.status(405).json({ ok: false, error: "Method not allowed" });
  }

  const { username, score, sessionId, sessionSeconds } = req.body;

  // ── 1. Basic type / presence validation ────────────────────────────────────
  if (typeof username !== "string" || username.trim().length === 0) {
    return res.status(400).json({ ok: false, error: "Invalid username" });
  }
  if (typeof score !== "number" || !Number.isInteger(score)) {
    return res.status(400).json({ ok: false, error: "Score must be an integer" });
  }
  if (typeof sessionId !== "string" || sessionId.length < 8) {
    return res.status(400).json({ ok: false, error: "Invalid sessionId" });
  }
  if (typeof sessionSeconds !== "number" || sessionSeconds < 0) {
    return res.status(400).json({ ok: false, error: "Invalid sessionSeconds" });
  }

  const cleanUsername = username.trim().substring(0, 32); // max 32 chars

  // ── 2. Score range check ───────────────────────────────────────────────────
  if (score <= 0) {
    // Score of 0 is not worth storing but not malicious — silently accept
    return res.status(200).json({ ok: true, note: "Score 0 not stored" });
  }
  if (score > MAX_SCORE) {
    console.warn(`[scoreValidation] Score ${score} exceeds MAX_SCORE ${MAX_SCORE} for user ${cleanUsername}`);
    return res.status(400).json({ ok: false, error: "Score exceeds maximum allowed value" });
  }

  // ── 3. Session time plausibility check ────────────────────────────────────
  // e.g. score=100 requires at least 150 seconds of gameplay
  const minRequiredSeconds = score * MIN_SECONDS_PER_POINT;
  if (sessionSeconds < minRequiredSeconds) {
    console.warn(`[scoreValidation] Implausible: score=${score} in ${sessionSeconds}s (need ${minRequiredSeconds}s) for user ${cleanUsername}`);
    return res.status(400).json({ ok: false, error: "Score not plausible for session duration" });
  }

  // ── 4. Duplicate session check (replay attack prevention) ─────────────────
  const sessionRef = db.ref(`_sessions/${sessionId}`);
  const sessionSnap = await sessionRef.once("value");
  if (sessionSnap.exists()) {
    console.warn(`[scoreValidation] Replay detected: sessionId ${sessionId} already used`);
    return res.status(400).json({ ok: false, error: "Session already submitted" });
  }

  // ── 5. Rate limit per player ───────────────────────────────────────────────
  // Key derived from username — simple, avoids needing auth
  const safeKey = cleanUsername.replace(/[.#$\[\]]/g, "_");
  const rateLimitRef = db.ref(`_rateLimit/${safeKey}`);
  const rateLimitSnap = await rateLimitRef.once("value");

  if (rateLimitSnap.exists()) {
    const lastSubmitMs = rateLimitSnap.val();
    const elapsedSeconds = (Date.now() - lastSubmitMs) / 1000;
    if (elapsedSeconds < RATE_LIMIT_SECONDS) {
      console.warn(`[scoreValidation] Rate limited: ${cleanUsername} (${elapsedSeconds.toFixed(1)}s since last submit)`);
      return res.status(429).json({ ok: false, error: "Submitting too fast — please wait" });
    }
  }

  // ── 6. All checks passed — write to leaderboard ───────────────────────────
  const now = Date.now();
  const key = db.ref("leaderboard").push().key;

  await Promise.all([
    // The real leaderboard entry
    db.ref(`leaderboard/${key}`).set({
      username: cleanUsername,
      score: score,
      timestamp: new Date(now).toISOString(),
      sessionSeconds: sessionSeconds,
    }),
    // Mark this session ID as used (TTL: 48h — cleaned up by a separate cron or DB rules)
    db.ref(`_sessions/${sessionId}`).set({ submittedAt: now }),
    // Update rate limit timestamp
    db.ref(`_rateLimit/${safeKey}`).set(now),
  ]);

  console.log(`[scoreValidation] Accepted: user=${cleanUsername}, score=${score}, session=${sessionId}`);
  return res.status(200).json({ ok: true });
});

// ── cleanupSessions ──────────────────────────────────────────────────────────
//
// Runs daily to delete _sessions entries older than 48 hours.
// Prevents the _sessions node from growing unboundedly.
//
const { onSchedule } = require("firebase-functions/v2/scheduler");

exports.cleanupSessions = onSchedule("every 24 hours", async () => {
  const cutoffMs = Date.now() - 48 * 60 * 60 * 1000; // 48 hours ago
  const sessionsRef = db.ref("_sessions");
  const snap = await sessionsRef.once("value");

  if (!snap.exists()) return;

  const deletions = [];
  snap.forEach((child) => {
    const val = child.val();
    const submittedAt = typeof val === "object" ? val.submittedAt : val;
    if (typeof submittedAt === "number" && submittedAt < cutoffMs) {
      deletions.push(child.ref.remove());
    }
  });

  await Promise.all(deletions);
  console.log(`[cleanupSessions] Removed ${deletions.length} stale session entries`);
});
