# CS2-SimplerAdmin

### Description
A de-bloated build of CS2-SimpleAdmin

### Commands
```js
- css_addadmin <steamid> <name> <flags/groups> <immunity> [time in minutes] - Add admin by steamid // @css/root
- css_deladmin <steamid> - Delete admin by steamid // @css/root
- css_ban <#userid or name> [time in minutes/0 perm] [reason] - Ban player // @css/ban
- css_addban <steamid> [time in minutes/0 perm] [reason] - Ban player via steamid64 // @css/ban
- css_banip <ip> [time in minutes/0 perm] [reason] - Ban player via IP address // @css/ban
- css_unban <steamid or name or ip> - Unban player // @css/unban
- css_kick <#userid or name> [reason] - Kick player / @css/kick
- css_give <#userid or name> <weapon> - Give weapon to player // @css/cheats
- css_rcon <command> - Run command as server // @css/rcon
```

### Requirements
- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp/) **tested on v225**
- MySQL **tested on MySQL (MariaDB) Server version: 10.11.4-MariaDB-1~deb12u1 Debian 12**


### Configuration
After first launch, u need to configure plugin in  addons/counterstrikesharp/configs/plugins/CS2-SimplerAdmin/CS2-SimplerAdmin.json


### Colors
```
        public static char Default = '\x01';
        public static char White = '\x01';
        public static char Darkred = '\x02';
        public static char Green = '\x04';
        public static char LightYellow = '\x03';
        public static char LightBlue = '\x03';
        public static char Olive = '\x05';
        public static char Lime = '\x06';
        public static char Red = '\x07';
        public static char Purple = '\x03';
        public static char Grey = '\x08';
        public static char Yellow = '\x09';
        public static char Gold = '\x10';
        public static char Silver = '\x0A';
        public static char Blue = '\x0B';
        public static char DarkBlue = '\x0C';
        public static char BlueGrey = '\x0D';
        public static char Magenta = '\x0E';
        public static char LightRed = '\x0F';
```
Use color name for e.g. {LightRed}

Credits for https://github.com/Hackmastr/css-basic-admin/
