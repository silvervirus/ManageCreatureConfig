using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Globalization;
using Newtonsoft.Json.Converters;
namespace SettingsManager
{

    public class SpawnConfiguration
    {
        public string CanSpawn { get; set; }
        public string SpawnChanceOutOf100 { get; set; }

    }
    public class Creature
    {
        public string Name { get; set; }
        public SpawnConfiguration SpawnConfiguration { get; set; }

    }
    public class UnwantedCreatures
    {
        public IList<Creature> Creature { get; set; }

    }
    public class Settings
    {
        public UnwantedCreatures UnwantedCreatures { get; set; }
        public List<Creature> UnwantedCreaturesList { get; set; } = new List<Creature>();
    }
    public class Application
    {
        public Settings Settings { get; set; }
       
    }
}
    
