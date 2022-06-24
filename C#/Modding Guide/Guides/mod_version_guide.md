# Mod Versioning

Any mod for version 0.3.4 and after of *Worlds* must contain a *version.json* file on it's root folder. This file must have the following structure:

```
{
  "version": <mod version>,
  "loader_version": <loader version>
}
```

The **version** entry contains the version of the mod. This is intended for mod creators to identify the version of the mod. It can follow any format but it is recommended if follow an incremental `<major>.<minor>` numbering format, where major identifies the major version number (for major mod changes or updates), and minor identifies the minor version number (for minor tweaks and fixes).

The **loader_version** entry must contain the earliest game version the mod is written for. So for example, a mod that was intended for version 0.3.4 of the game should have a *loader_version* equal to 0.3.4. The game will use the value to try to load the mod using backward compatibility if possible or alert the user if it's not possible to load the mod at all.
