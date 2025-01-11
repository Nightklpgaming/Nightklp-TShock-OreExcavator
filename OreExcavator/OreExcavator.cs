//terraria
using Terraria;
using TerrariaApi.Server;
using Terraria.GameContent.NetModules;
using Terraria.Localization;
using Terraria.Net;
using On.Terraria.Map;
using IL.Terraria.Map;
using Terraria.GameContent.Drawing;
//tshock
using TShockAPI;
using TShockAPI.Hooks;
using TShockAPI.DB;
using static Org.BouncyCastle.Math.EC.ECCurve;
using Terraria.ID;
using Terraria.DataStructures;
using System;
using System.Net;

namespace OreExcavator
{
    [ApiVersion(2, 1)]
    public class OreExcavator : TerrariaPlugin
    {
        #region [ Plugin Info ]
        public override string Author => "Nightklp";
        public override string Description => "Similar to tmodloader where you mined ore vein effortlessly";
        public override string Name => "OreExcavator";
        public override System.Version Version => new System.Version(1, 0);
        #endregion

        public static Config Config = Config.Read();

        public OreExcavator(Main game) : base(game)
        {
            //amogus
        }

        #region [ Initialize ]
        public override void Initialize()
        {
            ServerApi.Hooks.WorldSave.Register(this, OnWorldSave);

            GetDataHandlers.TileEdit += OnTileEdit;

            GeneralHooks.ReloadEvent += OnReload;
        }
        #endregion

        #region [ Dispose ]
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.WorldSave.Deregister(this, OnWorldSave);

                GetDataHandlers.TileEdit -= OnTileEdit;

                GeneralHooks.ReloadEvent -= OnReload;
            }
            base.Dispose(disposing);
        }
        #endregion

        #region [ Get Latest Version ]

        public async Task InformLatestVersion()
        {
            var http = HttpWebRequest.CreateHttp("https://raw.githubusercontent.com/Nightklpgaming/Nightklp-TShock-OreExcavator/master/version.txt");

            WebResponse res = await http.GetResponseAsync();

            using (StreamReader sr = new StreamReader(res.GetResponseStream()))
            {
                System.Version latestversion = new(sr.ReadToEnd());

                if (latestversion > Version)
                {
                    Console.WriteLine(Version.ToString(), latestversion.ToString());
                }

                return;
            }
        }

        #endregion

        private async void OnTileEdit(object? sender, GetDataHandlers.TileEditEventArgs args)
        {
            #region code
            if (!args.Player.HasPermission(Config.Main.Permission)) return;

            int tileX = args.X;
            int tileY = args.Y;

            Dictionary<ushort, short> Ores = GetOres();
            
            if (args.Action == GetDataHandlers.EditAction.KillTile && args.EditData == 0)
            {
                ushort tileid = Main.tile[tileX, tileY].type;

                if (Ores.ContainsKey(tileid))
                {
                    int maxtile = (int)Config.Main.MaxTilebreak;
                    int taskdelayms = (int)Config.Main.MilliSecondDelay;

                    ParticleOrchestraType particleType = (ParticleOrchestraType)Config.Main.particle;
                    bool usesound = (bool)Config.Main.WithSound;

                    await execute();
                    return;

                    async Task execute()
                    {
                        int totaltiles = 0;

                        Queue<(int x, int y)> queue = new Queue<(int x, int y)>();
                        HashSet<(int, int)> visited = new HashSet<(int, int)>();

                        queue.Enqueue((tileX, tileY));
                        visited.Add((tileX, tileY));

                        for (; queue.Count > 0;)
                        {
                            var (x, y) = queue.Dequeue();

                            if (Main.tile[x, y - 1].type is TileID.Containers or TileID.Containers2 or TileID.FakeContainers or TileID.FakeContainers2 or TileID.Dressers)
                                continue;

                            if (Main.tile[x, y] == null || !Main.tile[x, y].active() || Main.tile[x, y].type != tileid)
                                continue;
                            
                            if (maxtile <= totaltiles)
                            {
                                return;
                            }
                            
                            Main.tile[x, y].type = 0;
                            Main.tile[x, y].active(false);
                            NetMessage.SendTileSquare(args.Player.Index, x, y, 2);

                            ParticleOrchestraSettings settings = new()
                            {
                                IndexOfPlayerWhoInvokedThis = (byte)TSPlayer.Server.Index,
                                PositionInWorld = new((x * 16) + 8, (y * 16) + 8),
                                UniqueInfoPiece = -1
                            };
                            ParticleOrchestrator.BroadcastParticleSpawn(particleType, settings);

                            if (usesound)
                            {
                                NetMessage.PlayNetSound(new NetMessage.NetSoundInfo(new Microsoft.Xna.Framework.Vector2(x * 16, y * 16), 105, 1, 100, 0), -1, -1);
                            }

                            int num = Item.NewItem(new EntitySource_DebugCommand(), (int)(x * 16) + 8, (int)(y * 16) + 8 , args.Player.TPlayer.width, args.Player.TPlayer.height, Ores[tileid], 1, noBroadcast: true, 0, noGrabDelay: true);
                            Main.item[num].playerIndexTheItemIsReservedFor = args.Player.Index;
                            TSPlayer.All.SendData(PacketTypes.ItemDrop, "", num, 1f);
                            TSPlayer.All.SendData(PacketTypes.ItemOwner, null, num);

                            totaltiles++;

                            (int, int)[] neighbors = new (int, int)[]
                            {
                            (x + 1, y), // Right
                            (x - 1, y), // Left
                            (x, y + 1), // Down
                            (x, y - 1)  // Up
                            };

                            foreach (var (nx, ny) in neighbors)
                            {
                                if (visited.Contains((nx, ny)))
                                    continue;

                                if (Main.tile[nx, ny] != null && Main.tile[nx, ny].active() && Main.tile[nx, ny].type == tileid)
                                {
                                    queue.Enqueue((nx, ny));
                                    visited.Add((nx, ny));
                                }
                            }

                            await Task.Delay(taskdelayms);
                        }
                    }

                }
            }

            Dictionary<ushort, short> GetOres()
            {
                Dictionary<ushort, short> result = new();
                
                result.Add(TileID.Amethyst, ItemID.Amethyst);
                result.Add(TileID.Sapphire, ItemID.Sapphire);
                result.Add(TileID.Emerald, ItemID.Emerald);
                result.Add(TileID.Ruby, ItemID.Ruby);
                result.Add(TileID.Diamond, ItemID.Diamond);
                result.Add(TileID.Copper, ItemID.CopperOre);
                result.Add(TileID.Iron, ItemID.IronOre);
                result.Add(TileID.Silver, ItemID.SilverOre);
                result.Add(TileID.Gold, ItemID.GoldOre);
                result.Add(TileID.Tin, ItemID.TinOre);
                result.Add(TileID.Lead, ItemID.LeadOre);
                result.Add(TileID.Tungsten, ItemID.TungstenOre);
                result.Add(TileID.Platinum, ItemID.PlatinumOre);
                result.Add(TileID.Demonite, ItemID.DemoniteOre);
                result.Add(TileID.Crimtane, ItemID.CrimtaneOre);
                result.Add(TileID.Palladium, ItemID.PalladiumOre);
                result.Add(TileID.Cobalt, ItemID.CobaltOre);
                result.Add(TileID.Orichalcum, ItemID.OrichalcumOre);
                result.Add(TileID.Mythril, ItemID.MythrilOre);
                result.Add(TileID.Adamantite, ItemID.AdamantiteOre);
                result.Add(TileID.Titanium, ItemID.TitaniumOre);
                result.Add(TileID.Chlorophyte, ItemID.ChlorophyteOre);

                return result;
            }

            #endregion
        }

        private void OnReload(ReloadEventArgs args)
        {
            #region code
            Config = Config.Read();
            args.Player.SendInfoMessage("OreExcavator Config reloaded!");
            #endregion
        }

        private async void OnWorldSave(WorldSaveEventArgs args)
        {
            await InformLatestVersion();
        }
    }
}
