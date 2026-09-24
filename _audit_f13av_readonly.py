import json
from pathlib import Path
import pg8000.native

cs = json.loads(
    Path("D:/strike/Wuzzelo/wuzzlo/backend/app/Wuzzlo.GameService/appsettings.json").read_text()
)["ConnectionStrings"]["Postgres"]
p = {}
for i in cs.split(";"):
    if "=" in i:
        k, v = i.split("=", 1)
        p[k.strip().lower()] = v.strip()

conn = pg8000.native.Connection(
    user=p["username"],
    password=p["password"],
    host=p["host"],
    port=int(p["port"]),
    database=p["database"],
    ssl_context=False,
)

print("=== AviatorBets by Username F-13-av (group Operator via sessions/bets) ===")
# AviatorBets has Username + ExternalUserId but no OperatorId
rows = conn.run(
    """
    SELECT
      b."Username",
      b."ExternalUserId",
      COUNT(*) AS bet_count,
      MIN(b."PlacedAtUtc") AS earliest,
      MAX(b."PlacedAtUtc") AS latest
    FROM "AviatorBets" b
    WHERE b."Username" = :u
    GROUP BY b."Username", b."ExternalUserId"
    ORDER BY bet_count DESC
    """,
    u="F-13-av",
)
for r in rows:
    print(r)

print("=== Join GameSessions for OperatorId of those ExternalUserIds ===")
rows2 = conn.run(
    """
    SELECT
      s."OperatorId",
      s."Username",
      s."ExternalUserId",
      COUNT(*) AS session_count,
      BOOL_OR(s."IsActive") AS any_active,
      MIN(s."CreatedAtUtc") AS first_session,
      MAX(s."CreatedAtUtc") AS last_session,
      MAX(s."ExpiresAtUtc") AS max_expires
    FROM "GameSessions" s
    WHERE s."Username" = :u
    GROUP BY s."OperatorId", s."Username", s."ExternalUserId"
    ORDER BY s."OperatorId", last_session DESC
    """,
    u="F-13-av",
)
for r in rows2:
    print(r)

print("=== Cross-operator check: same Username other operators ===")
rows3 = conn.run(
    """
    SELECT DISTINCT s."OperatorId"
    FROM "GameSessions" s
    WHERE s."Username" = :u
    """,
    u="F-13-av",
)
print(rows3)

print("=== AviatorBets F-13-av with session OperatorId via GameSessionId ===")
rows4 = conn.run(
    """
    SELECT
      s."OperatorId",
      b."Username",
      b."ExternalUserId",
      COUNT(*) AS bet_count,
      MIN(b."PlacedAtUtc") AS earliest,
      MAX(b."PlacedAtUtc") AS latest
    FROM "AviatorBets" b
    INNER JOIN "GameSessions" s ON s."Id" = b."GameSessionId"
    WHERE b."Username" = :u
    GROUP BY s."OperatorId", b."Username", b."ExternalUserId"
    ORDER BY s."OperatorId", bet_count DESC
    """,
    u="F-13-av",
)
for r in rows4:
    print(r)

print("=== Username mismatch: same ExternalUserId, different Username (AviatorBets) ===")
rows5 = conn.run(
    """
    SELECT b."ExternalUserId", COUNT(DISTINCT b."Username") AS unames
    FROM "AviatorBets" b
    WHERE b."ExternalUserId" IN (
      SELECT DISTINCT "ExternalUserId" FROM "AviatorBets" WHERE "Username" = :u
    )
    GROUP BY b."ExternalUserId"
    HAVING COUNT(DISTINCT b."Username") > 1
    """,
    u="F-13-av",
)
print(rows5 or "none")

print("=== Index check: columns on AviatorBets sample ===")
cols = conn.run(
    """
    SELECT column_name FROM information_schema.columns
    WHERE table_name = 'AviatorBets'
    ORDER BY ordinal_position
    """
)
print([c[0] for c in cols])

print("=== Expired sessions still present for F-13-av? ===")
rows6 = conn.run(
    """
    SELECT
      COUNT(*) FILTER (WHERE "ExpiresAtUtc" <= NOW() AT TIME ZONE 'utc') AS expired_count,
      COUNT(*) FILTER (WHERE "ExpiresAtUtc" > NOW() AT TIME ZONE 'utc') AS unexpired_count,
      COUNT(*) FILTER (WHERE NOT "IsActive") AS inactive_count,
      COUNT(*) AS total
    FROM "GameSessions"
    WHERE "Username" = :u
    """,
    u="F-13-av",
)
print(rows6)

conn.close()
