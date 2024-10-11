# Where is my ride

This plugin tells you the location of the last ride the player disembarked from.

## Installing Plugin

1. Download the `MarkerAPI.cs` file from [Marker API](https://codefling.com/plugins/marker-api?tab=details).

1. Copy the `MarkerAPI.cs` file to `oxide/plugins` directory on the server.

1. Copy the `WhereIsMyRide.cs` file to `oxide/plugins` directory on the server.

### Plugin Settings

#### Configuration

- Configure plugin settings in `oxide/config/WhereIsMyRide.json`.

    | Key | Type | Default Value  | Description |
    | --- | --- | --- | --- |
    | "Add a marker on the map with the location of the ride" | bool | true |  |
    | "Send the location of the ride to chat" | bool | true |  |
    | "Map Marker Display Name" | string | "Your ride." |  |
    | "Map Marker Radius" | float | 0.4 |  |
    | "Map Marker Color" | string | "EE82EE" |  |
    | "Map Marker Color Outline" | string | "6A5ACD" |  |
    | "Time to display the marker on the map" | int | 30 |  |

#### Permissions

Allow players to use plugin.

- Allow players to use Where is my ride.
    ```
    oxide.grant user {PLAYER_ID} whereismyride.allow
    ```

## Using Plugin

1. Enter the following chat command in-game

    ```
    /where
    ```
