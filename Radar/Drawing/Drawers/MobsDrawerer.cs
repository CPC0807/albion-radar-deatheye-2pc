using VRise.Settings;
using VRise.Radar.GameObjects.Mobs;
using VRise.Radar.GameObjects.Players;
using VRise.Radar.Drawing.OverlaySettings;
using VRise.Radar.Utility;
using System.Numerics;
using GameOverlay.Drawing;
using System;
using System.Threading.Tasks;
using System.Linq;
using VRise.Radar.GameObjects.LocalPlayer;

namespace VRise.Radar.Drawers
{
    public class MobsDrawerer : IDrawerer
    {
        private readonly ConfigHandler configHandler;
        private readonly Graphics gfx;
        private readonly RadarOverlayBrushesDictionary brushesDictionary;

        private readonly LocalPlayerHandler localPlayerHandler;
        private readonly MobsHandler mobsHandler;

        public MobsDrawerer(Graphics gfx, RadarOverlayBrushesDictionary brushesDictionary,
            LocalPlayerHandler localPlayerHandler, MobsHandler mobsHandler)
        {
            configHandler = ConfigHandler.Source;
            this.gfx = gfx;
            this.brushesDictionary = brushesDictionary;

            this.localPlayerHandler = localPlayerHandler;
            this.mobsHandler = mobsHandler;
        }

        public async Task DrawAsync()
        {
            lock (mobsHandler.mobsList)
            {
                foreach (Mob m in mobsHandler.mobsList.Values)
                {
                    Vector2 pos = (m.Position - localPlayerHandler.localPlayer.Position).Rotate();

                    if (m.MobInfo != null)
                    {
                        switch (m.MobInfo.Type)
                        {
                            case "CORRUPTED_MOB":

                                foreach (int i in new int[] { 0, 1, 2, 3, 4 })
                                {
                                    if (Convert.ToBoolean(configHandler.config.CorruptedMobs[i]))
                                    {
                                        gfx.DrawIconDot(brushesDictionary._brushes["Black"],
                                            brushesDictionary._mobsImages[m.MobInfo.MobName], pos,
                                            Convert.ToSingle(configHandler.config.CorruptedMobs[5]));
                                        break;
                                    }
                                }

                                break;

                            case "CORRUPTED_TRAP":

                                foreach (int i in new int[] { 0, 1, 2, 3 })
                                {
                                    if (Convert.ToBoolean(configHandler.config.CorruptedTraps[i]))
                                    {
                                        gfx.DrawDotWithStringIcon(brushesDictionary._resourcesColors[8],
                                            brushesDictionary._resourcesHightlights[8],
                                            brushesDictionary._brushes["Black"], pos, brushesDictionary._fonts["Icon"],
                                            m.MobInfo.MobName,
                                            Convert.ToSingle(configHandler.config.CorruptedTraps[4]));

                                        switch (m.MobInfo.MobName)
                                        {
                                            case "FLAMESPINNER":
                                                gfx.DrawDotWithStringIcon(brushesDictionary._resourcesColors[8],
                                                    brushesDictionary._resourcesHightlights[8],
                                                    brushesDictionary._brushes["Black"], pos,
                                                    brushesDictionary._fonts["Icon"], "F",
                                                    Convert.ToSingle(configHandler.config.CorruptedTraps[4]));
                                                break;

                                            case "LAVATHROWER":
                                                gfx.DrawDotWithStringIcon(brushesDictionary._resourcesColors[8],
                                                    brushesDictionary._resourcesHightlights[8],
                                                    brushesDictionary._brushes["Black"], pos,
                                                    brushesDictionary._fonts["Icon"], "G",
                                                    Convert.ToSingle(configHandler.config.CorruptedTraps[4]));
                                                break;

                                            case "BOMBTHROWER":
                                                gfx.DrawDotWithStringIcon(brushesDictionary._resourcesColors[8],
                                                    brushesDictionary._resourcesHightlights[8],
                                                    brushesDictionary._brushes["Black"], pos,
                                                    brushesDictionary._fonts["Icon"], "H",
                                                    Convert.ToSingle(configHandler.config.CorruptedTraps[4]));
                                                break;

                                            case "SPIKES":
                                                gfx.DrawDotWithStringIcon(brushesDictionary._resourcesColors[8],
                                                    brushesDictionary._resourcesHightlights[8],
                                                    brushesDictionary._brushes["Black"], pos,
                                                    brushesDictionary._fonts["Icon"], "I",
                                                    Convert.ToSingle(configHandler.config.CorruptedTraps[4]));
                                                break;
                                        }

                                        break;
                                    }
                                }

                                break;

                            case "WORLD_BOSS":

                                if (Convert.ToBoolean(configHandler.config.WorldMobs[0]) && Convert.ToBoolean(configHandler.config.WorldMobs[1]))
                                {
                                    int rarity = Math.Min(Math.Max(m.MobInfo.Rarity, 0), 3); // 限制在 0-3 範圍
                                    gfx.DrawDotWithStringIcon(brushesDictionary._brushes["Black"],
                                        brushesDictionary._proksColors[rarity],
                                        brushesDictionary._brushes["Yellow"], pos, brushesDictionary._fonts["Main"],
                                        "B", Convert.ToSingle(configHandler.config.WorldMobs[4]));
                                }

                                break;

                            case "WORLD_MINIBOSS":

                                if (Convert.ToBoolean(configHandler.config.WorldMobs[0]) && Convert.ToBoolean(configHandler.config.WorldMobs[2]))
                                {
                                    int rarity = Math.Min(Math.Max(m.MobInfo.Rarity, 0), 3); // 限制在 0-3 範圍
                                    gfx.DrawDotWithStringIcon(brushesDictionary._brushes["Black"],
                                        brushesDictionary._proksColors[rarity],
                                        brushesDictionary._brushes["Yellow"], pos, brushesDictionary._fonts["Main"],
                                        "MB", Convert.ToSingle(configHandler.config.WorldMobs[4]));
                                }

                                break;

                            case "WORLD_CHAMPION":

                                if (Convert.ToBoolean(configHandler.config.WorldMobs[0]) && Convert.ToBoolean(configHandler.config.WorldMobs[3]))
                                {
                                    int rarity = Math.Min(Math.Max(m.MobInfo.Rarity, 0), 3); // 限制在 0-3 範圍
                                    gfx.DrawDotWithStringIcon(brushesDictionary._brushes["Black"],
                                        brushesDictionary._proksColors[rarity],
                                        brushesDictionary._brushes["Yellow"], pos, brushesDictionary._fonts["Main"],
                                        "C", Convert.ToSingle(configHandler.config.WorldMobs[4]));
                                }

                                break;

                            case "DRONE":

                                if (Convert.ToBoolean(configHandler.config.DroneMobs[0]))
                                {
                                    gfx.DrawIconDot(brushesDictionary._chargesColors[m.MobInfo.Rarity],
                                        brushesDictionary._mobsImages["DRONE"], pos,
                                        Convert.ToSingle(configHandler.config.DroneMobs[1]));
                                }

                                break;

                            case "MIST_PORTAL":

                                if (Convert.ToBoolean(configHandler.config.MistWisps[0]))
                                {
                                    if (m.MobInfo.Rarity != 0)
                                    {
                                        gfx.DrawIconDot(brushesDictionary._chargesColors[m.MobInfo.Rarity],
                                            brushesDictionary._mobsImages["MIST_PORTAL"], pos,
                                            Convert.ToSingle(configHandler.config.MistWisps[1]));
                                    }
                                    else
                                    {
                                        gfx.DrawIconDot(brushesDictionary._brushes["Black"],
                                            brushesDictionary._mobsImages["MIST_PORTAL"], pos,
                                            Convert.ToSingle(configHandler.config.MistWisps[1]));
                                    }

                                    gfx.DrawTextCentered(brushesDictionary._fonts["Main"],
                                        brushesDictionary._resourcesColors[8], pos.X,
                                        pos.Y + Convert.ToSingle(configHandler.config.MistWisps[1]) / 2 + 1.5f,
                                        m.MobInfo.Queue);
                                }

                                break;

                            case "MIST_BOSS":

                                if (Convert.ToBoolean(configHandler.config.MistMobs[0]))
                                {
                                    gfx.DrawIconDot(brushesDictionary._brushes["Yellow"],
                                        brushesDictionary._mobsImages[m.MobInfo.MobName], pos,
                                        Convert.ToSingle(configHandler.config.MistMobs[1]));

                                    Additions.DrawTextCentered(gfx, brushesDictionary._fonts["Main"],
                                        brushesDictionary._brushes["Yellow"], pos.X,
                                        pos.Y + Convert.ToSingle(configHandler.config.MistWisps[1]) / 2 + 1.5f,
                                        m.Health.strPercent);
                                }

                                break;

                            case "EVENT":

                                if (Convert.ToBoolean(configHandler.config.EventMobs[0]))
                                {
                                    gfx.DrawIconDot(brushesDictionary._brushes["Yellow"],
                                        brushesDictionary._mobsImages["EVENT"], pos,
                                        Convert.ToSingle(configHandler.config.EventMobs[1]));
                                }

                                break;

                            case "HIDDEN_TREASURE":

                                if (Convert.ToBoolean(configHandler.config.HiddenTreasures[0]))
                                {
                                    gfx.DrawIconDot(brushesDictionary._brushes["Black"],
                                        brushesDictionary._mobsImages["TREASURE"], pos,
                                        Convert.ToSingle(configHandler.config.EventMobs[1]));
                                }

                                break;

                            case "HARVESTABLE":

                                if (configHandler.config.ResourcesMobsEnabled)
                                {
                                    if (!configHandler.config.HarvestableList.Contains(
                                            $"T{m.MobInfo.Tier}-{m.Charge}-{m.MobInfo.HarvestableType}"))
                                    {
                                        continue;
                                    }

                                    if (m.MobInfo.Rarity == 0)
                                    {
                                        if (configHandler.config.OnlyAspectedMode) continue;

                                        if (m.Charge > 0)
                                        {
                                            Additions.DrawHarvestableDot(gfx,
                                                brushesDictionary._resoucesImages[m.MobInfo.HarvestableType],
                                                brushesDictionary._resourcesColors[m.MobInfo.Tier],
                                                brushesDictionary._chargesColors[m.Charge], pos,
                                                configHandler.config.HarvestableDotSize);
                                        }
                                        else
                                        {
                                            Additions.DrawHarvestableDot(gfx,
                                                brushesDictionary._resoucesImages[m.MobInfo.HarvestableType],
                                                brushesDictionary._resourcesColors[m.MobInfo.Tier],
                                                brushesDictionary._resourcesHightlights[m.MobInfo.Tier], pos,
                                                configHandler.config.HarvestableDotSize);
                                        }

                                        Additions.DrawTextCentered(gfx, brushesDictionary._fonts["Main"],
                                            brushesDictionary._resourcesColors[8], pos.X,
                                            pos.Y + configHandler.config.HarvestableDotSize / 2 + 1.5f, "Mob");
                                    }
                                    else
                                    {
                                        Additions.DrawHarvestableDot(gfx,
                                            brushesDictionary._resoucesImages[m.MobInfo.HarvestableType],
                                            brushesDictionary._resourcesColors[m.MobInfo.Tier],
                                            brushesDictionary._aspectedHighlightColors[m.MobInfo.Rarity], pos,
                                            configHandler.config.HarvestableDotSize);
                                        Additions.DrawTextCentered(gfx, brushesDictionary._fonts["Main"],
                                            brushesDictionary._resourcesColors[8], pos.X,
                                            pos.Y + configHandler.config.HarvestableDotSize / 2 + 1.5f, "Aspect");
                                    }
                                }

                                break;
                            default:
                                // NULL 類型（普通世界怪物）
                                if (Convert.ToBoolean(configHandler.config.WorldMobs[0]))
                                {
                                    gfx.DrawDotWithStringIcon(brushesDictionary._brushes["Black"],
                                        brushesDictionary._brushes["Gray"], brushesDictionary._brushes["Gray"], pos,
                                        brushesDictionary._fonts["Main"], "M",
                                        Convert.ToSingle(configHandler.config.WorldMobs[4]));
                                }
                                break;
                        }
                    }
                    else
                    {
                        // m.MobInfo == null 的情況 - 不顯示
                    }
                }
            }
        }
    }
}