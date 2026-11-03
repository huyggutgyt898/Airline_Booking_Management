USE AirlineBookingDB;
GO

-- The existing demo hashes are truncated and cannot be used to verify passwords.
-- This resets only the four known demo accounts to Demo@12345.
-- Run once in development; the length guard prevents resetting them again.
UPDATE dbo.Users
SET PasswordHash = CASE Username
    WHEN 'admin01' THEN 'AQAAAAIAAYagAAAAEFuSRhbjo/YyNRDvNrekryrM+1fFzzyolPyS1hFKx81SyxEgnKao+tiKxgHfLRuxXQ=='
    WHEN 'staff01' THEN 'AQAAAAIAAYagAAAAEHILRZJ7vKM++VQz5Tg8di4DP2n7jLnbSM9CxyISc9OV1OGWpG6vo30K0OvwhrtedA=='
    WHEN 'hieuhm' THEN 'AQAAAAIAAYagAAAAEP45tlJoGnEsdZQdY2wcRWfFMRfiS0c/a9sxIy+vTB0kXfyhtIgBl8t5QlD2Gum5EQ=='
    WHEN 'anhtuan' THEN 'AQAAAAIAAYagAAAAEErlWyIlpFecI4kNjbVnNtooRcDg0xrzrPw0LGd00X/e6YVC9Tc9QkTyhg4dotiqhw=='
END
WHERE Username IN ('admin01', 'staff01', 'hieuhm', 'anhtuan')
  AND PasswordHash LIKE 'AQAAAAEAACcQAAAAE%'
  AND LEN(PasswordHash) < 70;

SELECT @@ROWCOUNT AS DemoAccountsReset;
GO
