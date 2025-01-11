using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Newtonsoft.Json;
using TShockAPI;
using Terraria.ID;
using Terraria.GameContent.Drawing;

namespace OreExcavator
{
    public class Config
    {
        static string path = Path.Combine(TShock.SavePath, "OreExcavator.json");

        public CONFIG_MAIN Main;

        public static Config Read()
        {
            if (!File.Exists(path))
            {
                File.WriteAllText(path, JsonConvert.SerializeObject(Default(), Formatting.Indented));
                return Default();
            }


            var args = JsonConvert.DeserializeObject<Config>(File.ReadAllText(path));

            if (args == null) return Default();

            if (args.Main == null) args.Main = new();
            args.Main.FixNull();

            File.WriteAllText(path, JsonConvert.SerializeObject(args, Formatting.Indented));
            return args;
        }

        /// <summary>
        /// changes config file
        /// </summary>
        /// <param name="config"></param>
        public void Changeall()
        {
            if (!File.Exists(path))
            {
                File.WriteAllText(path, JsonConvert.SerializeObject(Default(), Formatting.Indented));
            }
            else
            {
                File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
            }
        }

        private static Config Default()
        {
            return new Config()
            {
                Main = new()
            };
        }
    }

    public class CONFIG_MAIN
    {
        public int? MaxTilebreak = 300;
        public int? MilliSecondDelay = 25;

        public ParticleOrchestraType? particle = ParticleOrchestraType.SilverBulletSparkle;
        public bool? WithSound = true;

        public string Permission = "";

        public CONFIG_MAIN() { }

        public void FixNull()
        {
            CONFIG_MAIN getdefault = new();

            if (MaxTilebreak == null) MaxTilebreak = getdefault.MaxTilebreak;
            if (MilliSecondDelay == null) MilliSecondDelay = getdefault.MilliSecondDelay;

            if (particle == null) particle = getdefault.particle;
            if (WithSound == null) WithSound = getdefault.WithSound;

            if (Permission == null) Permission = getdefault.Permission;

            return;
        }
    }
}
