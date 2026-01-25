# Settings

Location: src/settings/SettingRepository.cs

Overview
- Settings are stored and accessed via SettingRepository; components obtain configuration through the settings provider in InitializeAsync.
- Keep configuration keys documented next to the code that consumes them; do not duplicate key names across unrelated modules.

Updating settings
- Add new keys to the repository and provide defaults where appropriate.
- When changing persisted settings shape, provide a migration path in the repository implementation so existing installations upgrade cleanly.

Security
- Do not store secrets in plain text in settings; integrate platform secrets stores for production (Android keystore, Azure Key Vault, etc.).

Verification
- Confirm InitializeAsync signature and expected providers in src/settings/SettingRepository.cs before documenting new keys or behaviors.
