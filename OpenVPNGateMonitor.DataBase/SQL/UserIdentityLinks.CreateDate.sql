BEGIN;

-- 1) Temporarily disable user triggers (to prevent CreateDate override on UPDATE)
ALTER TABLE xgb_dashopnvpn."UserIdentityLinks" DISABLE TRIGGER ALL;
ALTER TABLE xgb_dashopnvpn."Users"            DISABLE TRIGGER ALL;

-- 2) Overwrite UserIdentityLinks.CreateDate from TelegramBotUsers.CreateDate
--    (no IS NULL guard => force overwrite)
UPDATE xgb_dashopnvpn."UserIdentityLinks" uil
SET "CreateDate" = tbu."CreateDate"
    FROM xgb_dashopnvpn."TelegramBotUsers" tbu
WHERE uil."ExternalId" ~ '^\s*\d+\s*$'
  AND tbu."TelegramId" = (btrim(uil."ExternalId"))::bigint;

-- 3) Overwrite Users.CreateDate from TelegramBotUsers.CreateDate (MIN per user)
WITH link_dates AS (
    SELECT
        uil."UserId",
        MIN(tbu."CreateDate") AS "CreateDateFromTg"
    FROM xgb_dashopnvpn."UserIdentityLinks" uil
             JOIN xgb_dashopnvpn."TelegramBotUsers"  tbu
                  ON uil."ExternalId" ~ '^\s*\d+\s*$'
    AND tbu."TelegramId" = (btrim(uil."ExternalId"))::bigint
GROUP BY uil."UserId"
    )
UPDATE xgb_dashopnvpn."Users" u
SET "CreateDate" = ld."CreateDateFromTg"
    FROM link_dates ld
WHERE u."Id" = ld."UserId";

-- 4) Verify
SELECT
    uil."UserId",
    u."Id"                          AS "UserId_Users",
    uil."ExternalId"                AS "ExternalId_Link(text)",
    tbu."TelegramId"                AS "ExternalId_Tg(bigint)",
    tbu."CreateDate"                AS "CreateDate_Tg",
    uil."CreateDate"                AS "CreateDate_Link_after",
    u."CreateDate"                  AS "CreateDate_User_after"
FROM xgb_dashopnvpn."UserIdentityLinks" uil
         JOIN xgb_dashopnvpn."TelegramBotUsers" tbu
              ON uil."ExternalId" ~ '^\s*\d+\s*$'
 AND tbu."TelegramId" = (btrim(uil."ExternalId"))::bigint
LEFT JOIN xgb_dashopnvpn."Users" u
ON u."Id" = uil."UserId"
ORDER BY uil."UserId"
    LIMIT 200;

-- 5) Re-enable triggers
ALTER TABLE xgb_dashopnvpn."UserIdentityLinks" ENABLE TRIGGER ALL;
ALTER TABLE xgb_dashopnvpn."Users"            ENABLE TRIGGER ALL;

COMMIT;
