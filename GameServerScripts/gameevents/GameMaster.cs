/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */
/*
 * Author:	GPORT
 * Date:	6/27/2022
 * This script should be put in /scripts/gameevents directory.
 * This event creates a Game Master NPC for tower defense.
 * The NPC controls herding of players to/from a finite game in the Proving Grounds.
 * It controls the creation and removal of mob waves.
 * It determines who wins and loses the game.
 */
using System;
using System.Reflection;
using System.Timers;
using DOL.GS.PacketHandler;
using DOL.Events;
using DOL.GS;
using DOL.AI;
using log4net;
using System.Collections.Generic;
using System.Collections;
using DOL.GS.Effects;
using DOL.GS.SkillHandler;
using DOL.Language;



namespace GameServerScripts.gameevents
{
    public class GameMasterEvent
    {
        /// <summary>
        /// Defines a logger for this class
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        #region NPC Class Declarations
        
        // Declare GameMasterNPC class which derives from GameNPC
        public class GameMasterNPC : GameNPC
        {
            // Empty constructor: sets the default parameters for this NPC
            public GameMasterNPC() : base()
            {
                Model = 2125;
                Size = 100;
                Level = 60;
                Realm = eRealm.None;                
                Heading = 3089;
                CurrentRegionID = 147;
                GuildName = "Queue Here";
            }

            // Right-click on Game Master
            public override bool Interact(GamePlayer player)
            {
                if (isSessionActive)
                {
                    SendReply(player, "A game is in progress.");
                    return true;
                }
                else
                { 
                    SendReply(player, player.Name + ", please choose an option:\n\n" +
                                        "Players in queue: " + playerQueue.Count + "\n" +
                                        "[Join Queue]\n" +
                                        "[Leave Queue]\n" +
                                        "[Start Game]");
                    return true;
                }
            }

            public override bool WhisperReceive(GameLiving source, string str)
            {
                if (!base.WhisperReceive(source, str)) return false;
                if (!(source is GamePlayer)) return false;
                if (isSessionActive) return false;               

                GamePlayer player = source as GamePlayer;
                TurnTo(player.X, player.Y);

                switch (str)
                {
                    case "Join Queue":
                        if (!playerQueue.Contains(player))
                        {
                            log.Info(playerQueue);
                            playerQueue.Add(player);
                            SendReply(player, "You have joined the queue. \n" +
                                        "Players in queue: " + playerQueue.Count);
                            log.Info(player.Name + " has joined the queue");
                            log.Info(playerQueue);
                        }
                        else
                        {
                            SendReply(player, "You are already in the queue."); 
                        }
                        break;
                    case "Leave Queue":
                       if (playerQueue.Contains(player)) {
                            log.Info(playerQueue);
                            playerQueue.Remove(player);
                            log.Info(playerQueue);
                            SendReply(player, "You have left the queue. \n" +
                                        "Players in queue: " + playerQueue.Count);
                            log.Info(player.Name + " has left the queue");
                            log.Info(playerQueue);
                        } else
                        {
                            SendReply(player, "You can't leave a queue that you aren't in.");
                        }
                        break;
                    case "Start Game":
                        if (playerQueue.Count > 1)
                        {
                            SendReply(player, "Starting game...");
                            log.Info("Starting custom game session.");
                            log.Info("Game Roster: " + playerQueue);
                            StartSession(player);
                        } else
                        {
                            SendReply(player, "Not enough players for a game!");
                        }
                                            
                        break;
                }
                return true;
            }

            // More attractive version of SayTo
            public void SendReply(GamePlayer player, string msg)
            {
                player.Out.SendMessage(msg, eChatType.CT_System, eChatLoc.CL_PopupWindow);
            }
        }

        #region Nexus Class Declaration
        public class AlbNexusNPC : GameGuard
        {
            //Empty constructor, sets the default parameters for this NPC
            public AlbNexusNPC() : base()
            {
                RespawnInterval = -1;
                Size = 150;
                Level = 60;
                Health = 10000;
                Realm = eRealm.Albion;
                IsWorthReward = true;
                CurrentRegionID = 234; //whatever proving ground is
            }
        }
        public class MidNexusNPC : GameGuard
        {
            //Empty constructor, sets the default parameters for this NPC
            public MidNexusNPC() : base()
            {
                RespawnInterval = -1;
                Size = 150;
                Level = 60;
                Health = 10000;
                Realm = eRealm.Midgard;
                IsWorthReward = true;
                CurrentRegionID = 234; //whatever proving ground is
            }
        }
        public class HibNexusNPC : GameGuard
        {
            //Empty constructor, sets the default parameters for this NPC
            public HibNexusNPC() : base()
            {
                RespawnInterval = -1;
                Size = 150;
                Level = 60;
                Health = 10000;
                Realm = eRealm.Hibernia;
                IsWorthReward = true;
                CurrentRegionID = 234; //whatever proving ground is
            }
        }
        #endregion Nexus Class Declaration

        #region Declare Minion (commented out for now)
        ////Declare melee NPCs
        //public class MinionNPC : GameNPC
        //{           
        //    //Empty constructor, sets the default parameters for this NPC
        //    public MinionNPC() : base()
        //    {
        //    }
        //}
        #endregion

        #endregion  NPC Class Declarations

        #region Game Objective Functions
        /// <summary>
        /// Starts the game
        /// </summary>
        private static void StartSession(GamePlayer player)
        {
            isSessionActive = true;
            ResetParticipantsStart();
            SpawnSessionNPCs();
            SetPlayerRealms();
            MovePlayersToSession(); //move and bind players in pks    

            m_sessionTimer = new Timer(1000);
            m_sessionTimer.AutoReset = true;
            m_sessionTimer.Elapsed += new ElapsedEventHandler(CheckGameEvents);
            m_sessionTimer.Start();

            //mob wave timer starts
            //m_waveTimer = new Timer(30000);
            //m_waveTimer.AutoReset = true;
            //m_waveTimer.Elapsed += new ElapsedEventHandler(CreateMobWave);
            //m_waveTimer.Start();
        }

        /// <summary>
        /// Ends the game
        /// </summary>
        private static void EndSession()
        {
            KillRemainingNPCs();
            MovePlayersFromSession();
            ResetParticipantsEnd();
            m_sessionTimer.Stop();
            m_sessionTimer.Close();
            //broadcast in region who won

            //m_waveTimer.Stop();
            //m_waveTimer.Close();
            isSessionActive = false;
            playerQueue.Clear();
        }

        /// <summary>
        /// Checks if game objectives have been completed
        /// </summary>
        public static void CheckGameEvents(object sender, ElapsedEventArgs args)
        {
            // Check if only one nexus is alive (win the game)
            if ((m_albNexus.IsAlive && !m_hibNexus.IsAlive && !m_midNexus.IsAlive)
                || (m_hibNexus.IsAlive && !m_albNexus.IsAlive && !m_midNexus.IsAlive)
                || (m_midNexus.IsAlive && !m_hibNexus.IsAlive && !m_albNexus.IsAlive))
            {
                LastNameWins();
                EndSession();
            }

            // Give each player passive income
            foreach (GameClient client in WorldMgr.GetClientsOfRegion(234))
            {
                client.Player.AddMoney(100);
            }


            // Turn losers into skeletons and make it so they can't participate in PVP
            foreach (var person in playerQueue)
            {
                if (!m_albNexus.IsAlive)
                {
                    if (person.Realm == eRealm.Albion)
                    {
                        person.StartInvulnerabilityTimer(2000, null);
                        person.Model = 2046;                       
                    }
                }
                else if (!m_hibNexus.IsAlive)
                {

                    if (person.Realm == eRealm.Hibernia)
                    {
                        person.StartInvulnerabilityTimer(2000, null);
                        person.Model = 2046;                        
                    }
                }
                else if (!m_midNexus.IsAlive)
                {
                    if (person.Realm == eRealm.Midgard)
                    {
                        person.StartInvulnerabilityTimer(2000, null);
                        person.Model = 2046;                       
                    }
                }
            }

            // Move player to region 234 if they login and they aren't in region 234 or 147
            //foreach (var person in playerQueue)
            //{ }
        }

        /// <summary>
        /// Puts a number for how many wins you have for last name (put in end of round)
        /// </summary>
        public static void LastNameWins()
        {
            foreach (var person in playerQueue)
            {
                if (m_albNexus.IsAlive)
                {
                    if (person.Realm == eRealm.Albion)
                    {
                        if (person.LastName != null)
                        {
                            int lastNameInt = Int32.Parse(person.LastName);
                            lastNameInt++;
                            person.LastName = lastNameInt.ToString();
                            
                        } else
                        {
                            person.LastName = "1";
                        }
                    }
                }
                else if (m_hibNexus.IsAlive)
                {

                    if (person.Realm == eRealm.Hibernia)
                    {
                        if (person.LastName != null)
                        {
                            int lastNameInt = Int32.Parse(person.LastName);
                            lastNameInt++;
                            person.LastName = lastNameInt.ToString();

                        }
                        else
                        {
                            person.LastName = "1";
                        }
                    }
                }
                else if (m_midNexus.IsAlive)
                {
                    if (person.Realm == eRealm.Midgard)
                    {
                        if (person.LastName != null)
                        {
                            int lastNameInt = Int32.Parse(person.LastName);
                            lastNameInt++;
                            person.LastName = lastNameInt.ToString();

                        }
                        else
                        {
                            person.LastName = "1";
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sets the realm of the players for a fair match (use playerQueue)
        /// </summary>
        private static void SetPlayerRealms()
        {
            var i = 0;
            if (playerQueue.Count % 2 == 0 && playerQueue.Count % 3 != 0) //divisible by 2 and not divisible by 3
            {
                foreach (var person in playerQueue)
                {
                    if (i == 0)
                    {
                        person.Realm = eRealm.Albion;
                    }
                    else if (i == 1)
                    {
                        person.Realm = eRealm.Midgard;
                    }

                    person.UpdatePlayerStatus();

                    i++;
                    if (i > 1)
                    {
                        i = 0;
                    }
                }
            }
            else
            {
                foreach (var person in playerQueue)
                {
                    if (i == 0)
                    {
                        person.Realm = eRealm.Albion;
                    }
                    else if (i == 1)
                    {
                        person.Realm = eRealm.Midgard;
                    }
                    else if (i == 2)
                    {
                        person.Realm = eRealm.Hibernia;
                    }

                    person.UpdatePlayerStatus();

                    i++;
                    if (i > 2)
                    {
                        i = 0;
                    }
                }
            }

        }

        /// <summary>
        /// Ports a player to the correct PK
        /// </summary>
        private static void MovePlayersToSession()
        {
            foreach (var person in playerQueue)
            {
                if (person.Realm == eRealm.Albion)
                {
                    person.MoveTo(234, 572689, 549687, 8640, 731);
                    person.Bind(true);
                }
                else if (person.Realm == eRealm.Midgard)
                {
                    person.MoveTo(234, 557159, 573161, 8640, 2068);
                    person.Bind(true);
                }
                else if (person.Realm == eRealm.Hibernia)
                {
                    person.MoveTo(234, 541523, 549946, 8640, 3371);
                    person.Bind(true);
                }
            }
        }

        /// <summary>
        /// Spawn all mobs in battleground
        /// </summary>
        private static void SpawnSessionNPCs()
        {
            // ALB Nexus instance
            m_albNexus = new AlbNexusNPC();
            m_albNexus.X = 572108;
            m_albNexus.Y = 550063;
            m_albNexus.Z = 8640;
            m_albNexus.Heading = 731;
            m_albNexus.Model = 2151;
            m_albNexus.Name = "Albion King";

            // MID Nexus instance
            m_midNexus = new MidNexusNPC();
            m_midNexus.X = 557070;
            m_midNexus.Y = 572491;
            m_midNexus.Z = 8640;
            m_midNexus.Heading = 2068;
            m_midNexus.Model = 2168;
            m_midNexus.Name = "Midgard King";

            // HIB Nexus instance
            m_hibNexus = new HibNexusNPC();
            m_hibNexus.X = 542193;
            m_hibNexus.Y = 550173;
            m_hibNexus.Z = 8640;
            m_hibNexus.Heading = 3371;
            m_hibNexus.Model = 2152;
            m_hibNexus.Name = "Hibernia King";

            m_albNexus.AddToWorld();
            m_midNexus.AddToWorld();
            m_hibNexus.AddToWorld();

            // If there are 2 realms playing then the hib nexus begins the match dead
            if ((playerQueue.Count % 2 == 0 && playerQueue.Count % 3 != 0)) // divisible by 2, not divisible by 3
            {
                m_hibNexus.ChangeHealth(m_hibNexus, GameLiving.eHealthChangeType.Unknown, -999999);
                m_hibNexus.Model = 1438;
            }
        }

        /// <summary>
        /// Reset players at the start of the game (so they can enjoy the items in the lobby after a match - they can go to dueling/practice zone)
        /// </summary>
        private static void ResetParticipantsEnd()
        {
            foreach (var person in playerQueue)
            {
                // Reset invulnerability
                person.StartInvulnerabilityTimer(1, null);
                // Reset player model
                person.Model = (ushort)person.Client.Account.Characters[person.Client.ActiveCharIndex].CreationModel;
            }
        }

        /// <summary>
        /// Reset players at the start of the game (so they can enjoy the items in the lobby after a match - they can go to dueling/practice zone)
        /// </summary>
        private static void ResetParticipantsStart()
        {
            foreach (var person in playerQueue)
            {
                // Reset money
                long currentCash = person.GetCurrentMoney();
                person.RemoveMoney(currentCash);
                //delete inventories/equipment (except for starter items)
                //add back and equip starter equipment
                //reset RPS
            }
        }

        /// <summary>
        /// Ports players back to Game Lobby and binds them
        /// </summary>
        private static void MovePlayersFromSession()
        {
            foreach (var person in playerQueue)
            {
                person.Release(GamePlayer.eReleaseType.Normal, true);
                person.MoveTo(147, 42803, 38478, 10225, 1023);
                person.Bind(true);
            }
        }

        /// <summary>
        /// Kills all instance mobs in game session (for reset purposes)
        /// </summary>
        private static void KillRemainingNPCs()
        {
            if (m_albNexus != null)
                m_albNexus.Delete();
            if (m_midNexus != null)
                m_midNexus.Delete();
            if (m_hibNexus != null)
                m_hibNexus.Delete();
        }

        #endregion Game Objective Functions

        #region Variables
        // Randomness to use
        private static Random m_rnd;
        
        // GameMasterNPC who will initiates the game session
        private static GameMasterNPC m_gameMaster;

        // Nexuses
        private static AlbNexusNPC m_albNexus;
        private static MidNexusNPC m_midNexus;
        private static HibNexusNPC m_hibNexus;

        // Game session timer
        private static Timer m_sessionTimer;

        // List of players in game/queue
        private static List<GamePlayer> playerQueue = new List<GamePlayer>();       

        // Bool for if a game session is active
        private static bool isSessionActive = false;

        #region Minion Variables (commented out for now)
        // Minion
        //private static MinionNPC m_minion;
        //private static INpcTemplate meleeTemplate = NpcTemplateMgr.GetTemplate(999999990);
        //private static INpcTemplate casterTemplate = NpcTemplateMgr.GetTemplate(999999991);

        // Minion wave timers
        //private static Timer m_waveTimer;
        //private static Timer m_soloTimer;

        // Ints for minion waves
        //private static int i = 0; //counter start at 0
        //private static int meleeSize = 1; //number of melee mobs to make (front line), remainder of wave are casters
        //private static int waveSize = 2; //max mobs per wave
        #endregion

        #endregion

        #region Minion Wave Control (commented out for now)
        ////timercallback function fired to start every mob wave
        //protected static void CreateMobWave(object sender, ElapsedEventArgs args) //FIRING EVERY MINUTE
        //{
        //    m_soloTimer = new Timer(1000);
        //    m_soloTimer.AutoReset = true;
        //    m_soloTimer.Elapsed += new ElapsedEventHandler(CreateMobSolo);
        //    m_soloTimer.Start();
        //}

        ////timercallback function fired to make each individual mob of a mob wave
        //protected static void CreateMobSolo(object sender, ElapsedEventArgs args)
        //{
        //    i++; //increases by 1 per timer elapse

        //    //make melee mobs where # = meleeSize
        //    if (i <= meleeSize)
        //    {
        //        if (m_albNexus.IsAlive && m_hibNexus.IsAlive)
        //        {
        //            SpawnMinion(eRealm.Hibernia, eRealm.Albion, "melee");
        //            SpawnMinion(eRealm.Albion, eRealm.Hibernia, "melee");
        //        }

        //        if (m_albNexus.IsAlive && m_midNexus.IsAlive)
        //        {
        //            SpawnMinion(eRealm.Albion, eRealm.Midgard, "melee");
        //            SpawnMinion(eRealm.Midgard, eRealm.Albion, "melee");
        //        }

        //        if (m_midNexus.IsAlive && m_hibNexus.IsAlive)
        //        {
        //            SpawnMinion(eRealm.Hibernia, eRealm.Midgard, "melee");
        //            SpawnMinion(eRealm.Midgard, eRealm.Hibernia, "melee");                
        //        }
        //    }
        //    //make  caster mobs from remainder of i count
        //    else
        //    {
        //        if (m_albNexus.IsAlive && m_hibNexus.IsAlive)
        //        {
        //            SpawnMinion(eRealm.Hibernia, eRealm.Albion, "caster");
        //            SpawnMinion(eRealm.Albion, eRealm.Hibernia, "caster");
        //        }

        //        if (m_albNexus.IsAlive && m_midNexus.IsAlive)
        //        {
        //            SpawnMinion(eRealm.Albion, eRealm.Midgard, "caster");
        //            SpawnMinion(eRealm.Midgard, eRealm.Albion, "caster");
        //        }

        //        if (m_midNexus.IsAlive && m_hibNexus.IsAlive)
        //        {
        //            SpawnMinion(eRealm.Hibernia, eRealm.Midgard, "caster");
        //            SpawnMinion(eRealm.Midgard, eRealm.Hibernia, "caster");
        //        }
        //    }

        //    //when i meets the limit the solo timer stops, and no more mobs are made in this wave
        //    if (i >= waveSize)
        //        if (m_soloTimer != null)
        //        {
        //            i = 0;//reset i to 0
        //            m_soloTimer.Stop();
        //            m_soloTimer.Close();
        //        }
        //}

        ////creates single melee minion
        //protected static void SpawnMinion(eRealm realmSource, eRealm realmTarget, string mobType)//mobType can be "melee" or "caster"
        //{
        //    //instantiate mob object
        //    m_minion = new MinionNPC();

        //    //minion type
        //    if (mobType == "melee")
        //    {
        //        m_minion.LoadTemplate(meleeTemplate);//apply NPC template
        //        m_minion.Name = "Melee";//name mob            
        //    } else
        //    {
        //        m_minion.LoadTemplate(casterTemplate);//apply NPC template
        //        m_minion.Name = "Caster";//name mob            
        //    }

        //    //basic minion properties
        //    m_minion.RespawnInterval = -1;
        //    m_minion.Level = 40;
        //    m_minion.IsWorthReward = true;
        //    m_minion.Z = 8640;
        //    m_minion.CurrentRegionID = 234;

        //    //realm source conditionals
        //    if (realmSource == eRealm.Hibernia)
        //    {
        //        m_minion.GuildName = "Hibernia";
        //        m_minion.Realm = realmSource;
        //        //model
        //        if (mobType == "melee"){m_minion.Model = 881;}
        //            else {m_minion.Model = 2152;}
        //        //spawnpoint
        //        m_minion.X = 542193 + m_rnd.Next(40);
        //        m_minion.Y = 550173 + m_rnd.Next(40);                
        //    }
        //    else if (realmSource == eRealm.Albion)
        //    {
        //        m_minion.GuildName = "Albion";
        //        m_minion.Realm = realmSource;
        //        //model
        //        if (mobType == "melee"){m_minion.Model = 880;}
        //            else {m_minion.Model = 2151;}
        //        //spawnpoint
        //        m_minion.X = 572108 + m_rnd.Next(40);
        //        m_minion.Y = 550063 + m_rnd.Next(40);
        //    }
        //    else if (realmSource == eRealm.Midgard)
        //    {
        //        m_minion.GuildName = "Midgard";
        //        m_minion.Realm = realmSource;
        //        //model
        //        if (mobType == "melee"){m_minion.Model = 883;}
        //            else {m_minion.Model = 2168;}
        //        //spawnpoint
        //        m_minion.X = 557070 + m_rnd.Next(40);
        //        m_minion.Y = 572491 + m_rnd.Next(40);
        //    }
        //    else
        //    {
        //        return;
        //    }

        //    //finally add mob to world
        //    m_minion.AddToWorld();

        //    //send mob to fighting zone
        //    WalkToNexus(realmTarget);
        //}

        //public static void WalkToNexus(eRealm realmTarget)
        //{
        //    if (realmTarget == eRealm.Albion)
        //    {
        //        m_minion.WalkTo(572108, 550063, 8640, 400);
        //    }
        //    else if (realmTarget == eRealm.Midgard)
        //    {
        //        m_minion.WalkTo(557070, 572491, 8640, 400);
        //    }
        //    else if (realmTarget == eRealm.Hibernia)
        //    {
        //        m_minion.WalkTo(542193, 550173, 8640, 400);
        //    }
        //}
        #endregion

        //This function will be called to initialize the event
        //It needs to be declared in EVERY game-event and is used
        //to start things up.
        [ScriptLoadedEvent]
        public static void OnScriptsCompiled(DOLEvent e, object sender, EventArgs args)
        {
            // Declare random instance so it is usable
            m_rnd = new Random();

            // Game Master instance
            m_gameMaster = new GameMasterNPC();
            m_gameMaster.Faction = FactionMgr.GetFactionByID(0);
            m_gameMaster.X = 42079;
            m_gameMaster.Y = 38399;
            m_gameMaster.Z = 10341;
            m_gameMaster.Name = "Game Master";            

            // Add Game Master to the world
            bool good = true;
            if (!m_gameMaster.AddToWorld())
                good = false;            

            // Logging
            if (log.IsInfoEnabled)
                if (log.IsInfoEnabled)
                    log.Info("GameMasterNPCEvent initialized: " + good);           
        }

        // This function is called whenever the event is stopped. It should be used to clean up!
        [ScriptUnloadedEvent]
        public static void OnScriptUnload(DOLEvent e, object sender, EventArgs args)
        {
            // Delete our Game Master from the world
            if (m_gameMaster != null)
                m_gameMaster.Delete();
        }
    }
}