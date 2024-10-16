# Electrician

This plugin upgrades the wire tool and streamlines the electrician's work.

## Features

- Invisible wires can be installed.
- Displays power information for electrical items.

## Installing Plugin

1. Copy the `Electrician.cs` file to `oxide/plugins` directory on the server.

### Plugin Settings

#### Configuration

- Configure plugin settings in `oxide/config/Electrician.json`.

    | Key | Type | Default Value  | Description |
    | --- | --- | --- | --- |
    | "GUI refresh interval" | float | 1.0 | Specifies the interval at which the GUI, such as the power information, is refreshed. |

#### Permissions

Allow players to use plugin.

- Allow players to use invisible wire.

    ```
    oxide.grant user {PLAYER_ID} electrician.invisiblewire.allow
    ```

- Allow players to use power information.

    ```
    oxide.grant user {PLAYER_ID} electrician.powerinfo.allow
    ```

## Using Plugin

1. Specify the visibility of a wire by typing the chat command `elec.wire` in the in-game chat. The argument of the command can be `visible` or `invisible`.

    ```
    /elec.wire visible
    ```

1. Specify the visibility of a power information by typing the chat command `elec.powerinfo` in the in-game chat. The argument of the command can be `visible` or `invisible`.

    ```
    /elec.powerinfo visible
    ```

1. Hold the wire tool in your hand in the game.
