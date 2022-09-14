﻿using CitiesHarmony.API;
using ICities;

namespace ElevatedStopsEnabler
{
    public class Mod : IUserMod
    {
        public string Name => "Elevated Stops Enabler [Plazas&Promenades fix]";

        public string Description => "Allows to place transport stops on elevated versions of roads";
        
        public void OnEnabled() {
            HarmonyHelper.EnsureHarmonyInstalled();
        }
    }
}
