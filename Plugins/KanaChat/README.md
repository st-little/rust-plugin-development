# Kana Chat

Japanese Romaji typed in chat will be converted to Japanese Hiragana using [WanaKana](https://wanakana.com/).

## Installing Plugin

1. Build [WanaKanaShaapu](https://github.com/kmoroz/WanaKanaShaapu/releases) and create a `WanaKanaShaapu.dll` file.

1. Copy the `WanaKanaShaapu.dll` file to `RustDedicated_Data/Managed` directory on the server.

1. Download the `BetterChat.cs` file from [Better Chat](https://umod.org/plugins/better-chat).

1. Copy the `BetterChat.cs` file to `oxide/plugins` directory on the server.

1. Copy the `KanaChat.cs` file to `oxide/plugins` directory on the server.

### Plugin Settings

#### Configuration

Configure plugin settings in `oxide/config/KanaChat.json`.

| Key | Type | Default | Description |
| --- | --- | --- | --- |
| ProhibitedWords | List\<string> |  | Words set as prohibited words are masked. |
| IgnoreWords | List\<string> |  | Words set to ignore words skip kana conversion. |

#### Permissions

Allow players to use plugin.

- [Allow players to use Better Chat.](https://umod.org/plugins/better-chat#permissions)
- Allow players to use Kana Chat.
    ```
    oxide.grant user {PLAYER_ID} kanachat.allow
    ```

## Using Plugin

1. Type Japanese romaji in Team Chat or Global Chat.