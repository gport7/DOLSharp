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

namespace GameServerScripts.gameevents
{
    //Declare our event class
    public class GameMasterEvent
    {
        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        #region NPCs
        //Declare our GameMasterNPC class which derives from GameNPC (and inherits all it's functionality)
        public class GameMasterNPC : GameNPC
        {
            //Empty constructor, sets the default parameters for this NPC
            public GameMasterNPC() : base()
            {
                Model = 2125;
                Size = 100;
                Level = 60;
                Realm = 0;
                Heading = 3089;
                CurrentRegionID = 147;
                GuildName = "Queue Here";
            }

            //Right-click on Game Master
            public override bool Interact(GamePlayer player)
            {
                if (!base.Interact(player))
                    return false;

                //if (game is happening right now)
                //SendReply(player, "There is an active game session. You cannot queue.\n\n" +
                //    "It has been going on for " + m_waveTimer(whatevercommand) + " time.");


                //get int for players in region for y
                //get int for players in queue for x from the 2d array (playerQueue.length)
                SendReply(player, "There are X/Y players in the queue/lobby.\n\n" +
                                    player.Name + ", please choose an option:\n\n" +
                                    "[Join Queue]\n" +
                                    "[Leave Queue]\n" +
                                    "[Start Game] (for testing purposes)");
                return true;
            }

            public override bool WhisperReceive(GameLiving source, string str)
            {
                if (!base.WhisperReceive(source, str)) return false;
                if (!(source is GamePlayer)) return false;
                //if (game is happening right now) return false;

                GamePlayer player = source as GamePlayer;
                TurnTo(player.X, player.Y);

                switch (str)
                {
                    case "Join Queue":
                        //IF the player is not already in the queue:
                        //add the player to 2 dimensional array for the queue
                        //get int for players in region for y
                        //get int for players in queue for x from the 2d array
                        SendReply(player, "You have joined the queue. \n" +
                                    "There are X/Y players in the queue/lobby.");
                        log.Info(player.Name + " has joined the queue");
                        log.Info("Full queue: " + playerQueue);
                        //if the number of players in the queue vs number in lobby is correct, then start the game
                        //StartSession();
                        //(clear this queue when the game session is over)

                        //ELSE
                        //SendReply(player, "You are already in the queue."); 
                        break;
                    case "Leave Queue":
                        //IF the player is in the queue:
                        //remove the player from 2 dimensional array for the queue
                        //get int for players in region for y
                        //get int for players in queue for x from the 2d array
                        SendReply(player, "You have left the queue. \n" +
                                    "There are X/Y players in the queue/lobby.");
                        log.Info(player.Name + " has left the queue");
                        log.Info("Full queue: " + playerQueue);

                        //ELSE
                        //SendReply(player, "You can't leave a queue that you aren't in.");
                        break;
                    case "Start Game":
                        //IF the player is in the queue:
                        //remove the player from 2 dimensional array for the queue
                        //get int for players in region for y
                        //get int for players in queue for x from the 2d array
                        SendReply(player, "Starting Game");
                        log.Info("Starting Tower Defense game session.");
                        log.Info("Full queue: " + playerQueue);
                        StartSession();
                        //player.MoveTo(); //battlegrounds
                        break;
                }
                return true;
            }

            //sets the realm of the players for a fair match
            private static void SetPlayerRealms(int[,] playersInQueue)
            {
                
            }

            //ports the players out to the battlegrounds for the game session
            private static void MovePlayersToSession(int[,] playersInQueue)
            {

            }        

            //start the game session
            private static void StartSession()
            {                
                //set isSessionActive to true
                //for each player in queue, SendReply(the game is starting)
                //reset inventories/equipped items
                //reset RPs
                //reset money            
                //set player realms
                //for each player in queue, player.MoveTo to BG
                //m_waveTimer.Start();
            }

            //cleaner than SayTo
            public void SendReply(GamePlayer player, string msg)
            {
                player.Out.SendMessage(msg, eChatType.CT_System, eChatLoc.CL_PopupWindow);
            }
        }

        //Declare melee NPCs
        public class MinionNPC : GameNPC
        {           
            //Empty constructor, sets the default parameters for this NPC
            public MinionNPC() : base()
            {                
            }
        }

        //Declare nexuses
        public class AlbNexusNPC : GameNPC
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
        public class MidNexusNPC : GameNPC
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
        public class HibNexusNPC : GameNPC
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
        #endregion     

        #region variables
        //some randomness to use wherever
        private static Random m_rnd;
        //one GameMasterNPC who will control the game session
        private static GameMasterNPC m_gameMaster;
        //a minion
        private static MinionNPC m_minion;
        private static INpcTemplate meleeTemplate = NpcTemplateMgr.GetTemplate(999999990);
        private static INpcTemplate casterTemplate = NpcTemplateMgr.GetTemplate(999999991);
        //the nexuses
        private static AlbNexusNPC m_albNexus;
        private static MidNexusNPC m_midNexus;
        private static HibNexusNPC m_hibNexus;
        //timers to time the creation of mob waves
        private static Timer m_waveTimer;
        private static Timer m_soloTimer;
        //ints for mobs per wave
        private static int i = 0; //counter start at 0
        private static int meleeSize = 6; //number of melee mobs to make (front line), remainder of wave are casters
        private static int waveSize = 10; //max mobs per wave
        //2d array for players (player,realm (to be changed to proper realm on start), minionScore (start at 0, add per minion kill))
        private static int[,] playerQueue;
        //bool for if a game is active
        private static bool isSessionActive = false;
        #endregion


        //This function will be called to initialize the event
        //It needs to be declared in EVERY game-event and is used
        //to start things up.
        [ScriptLoadedEvent]
        public static void OnScriptsCompiled(DOLEvent e, object sender, EventArgs args)
        {
            //We set a new random class (this will move to StartSession() when start moves)
            m_rnd = new Random();

            //create Game Master instance
            m_gameMaster = new GameMasterNPC();
            m_gameMaster.X = 42079;
            m_gameMaster.Y = 38399;
            m_gameMaster.Z = 10341;
            m_gameMaster.Name = "Game Master";

            //create ALB Nexus instance
            m_albNexus = new AlbNexusNPC();
            m_albNexus.X = 572108;
            m_albNexus.Y = 550063;
            m_albNexus.Z = 8640;
            m_albNexus.Heading = 731;
            m_albNexus.Model = 34;
            m_albNexus.Name = "Albion King";

            //create MID Nexus instance
            m_midNexus = new MidNexusNPC();
            m_midNexus.X = 557070;
            m_midNexus.Y = 572491;
            m_midNexus.Z = 8640;
            m_midNexus.Heading = 2068;
            m_midNexus.Model = 34;
            m_midNexus.Name = "Midgard King";

            //create HIB Nexus instance
            m_hibNexus = new HibNexusNPC();
            m_hibNexus.X = 542193;
            m_hibNexus.Y = 550173;
            m_hibNexus.Z = 8640;
            m_hibNexus.Heading = 3371;
            m_hibNexus.Model = 34;
            m_hibNexus.Name = "Hibernia King";

            //add Game Master to the world
            bool good = true;
            if (!m_gameMaster.AddToWorld())
                good = false;

            //add Nexuses to world
            m_albNexus.AddToWorld();
            m_midNexus.AddToWorld();
            m_hibNexus.AddToWorld();

            //logging
            if (log.IsInfoEnabled)
                if (log.IsInfoEnabled)
                    log.Info("GameMasterNPCEvent initialized: " + good);

            ///////////////////BELOW THIS LINE (within these brackets) MOVES TO NEW SECTION/////////////////

            //timer that will make mob waves every x milliseconds (move to session start) (remember to have a session end and close this)
            m_waveTimer = new Timer(30000);
            m_waveTimer.AutoReset = true;
            m_waveTimer.Elapsed += new ElapsedEventHandler(CreateMobWave);
            m_waveTimer.Start(); //this will move to session start method
        }        

        //timercallback function fired to start every mob wave
        protected static void CreateMobWave(object sender, ElapsedEventArgs args) //FIRING EVERY MINUTE
        {
            m_soloTimer = new Timer(1000);
            m_soloTimer.AutoReset = true;
            m_soloTimer.Elapsed += new ElapsedEventHandler(CreateMobSolo);
            m_soloTimer.Start();
        }

        //timercallback function fired to make each individual mob of a mob wave
        protected static void CreateMobSolo(object sender, ElapsedEventArgs args)
        {
            i++; //increases by 1 per timer elapse

            //make melee mobs where # = meleeSize
            if (i <= meleeSize)
            {
                if (m_albNexus.IsAlive && m_hibNexus.IsAlive)
                {
                    SpawnMinion(eRealm.Hibernia, eRealm.Albion, "melee");
                    SpawnMinion(eRealm.Albion, eRealm.Hibernia, "melee");
                }

                if (m_albNexus.IsAlive && m_midNexus.IsAlive)
                {
                    SpawnMinion(eRealm.Albion, eRealm.Midgard, "melee");
                    SpawnMinion(eRealm.Midgard, eRealm.Albion, "melee");
                }

                if (m_midNexus.IsAlive && m_hibNexus.IsAlive)
                {
                    SpawnMinion(eRealm.Hibernia, eRealm.Midgard, "melee");
                    SpawnMinion(eRealm.Midgard, eRealm.Hibernia, "melee");                
                }
            }
            //make  caster mobs from remainder of i count
            else
            {
                if (m_albNexus.IsAlive && m_hibNexus.IsAlive)
                {
                    SpawnMinion(eRealm.Hibernia, eRealm.Albion, "caster");
                    SpawnMinion(eRealm.Albion, eRealm.Hibernia, "caster");
                }

                if (m_albNexus.IsAlive && m_midNexus.IsAlive)
                {
                    SpawnMinion(eRealm.Albion, eRealm.Midgard, "caster");
                    SpawnMinion(eRealm.Midgard, eRealm.Albion, "caster");
                }

                if (m_midNexus.IsAlive && m_hibNexus.IsAlive)
                {
                    SpawnMinion(eRealm.Hibernia, eRealm.Midgard, "caster");
                    SpawnMinion(eRealm.Midgard, eRealm.Hibernia, "caster");
                }
            }

            //when i meets the limit the solo timer stops, and no more mobs are made in this wave
            if (i >= waveSize)
                if (m_soloTimer != null)
                {
                    i = 0;//reset i to 0
                    m_soloTimer.Stop();
                    m_soloTimer.Close();
                }
        }

        //creates single melee minion
        protected static void SpawnMinion(eRealm realmSource, eRealm realmTarget, string mobType)//mobType can be "melee" or "caster"
        {
            //instantiate mob object
            m_minion = new MinionNPC();//instantiate
            if (mobType == "melee")
            {
                m_minion.LoadTemplate(meleeTemplate);//apply NPC template
                m_minion.Name = "Melee";//name mob            
            } else
            {
                m_minion.LoadTemplate(casterTemplate);//apply NPC template
                m_minion.Name = "Caster";//name mob            
            }
            //basic minion properties
            m_minion.RespawnInterval = -1;
            m_minion.Health = 300;
            m_minion.IsWorthReward = true;
            m_minion.Z = 8640;
            m_minion.CurrentRegionID = 234;

            //realm source conditionals
            if (realmSource == eRealm.Hibernia)
            {
                m_minion.GuildName = "Hibernia";
                m_minion.Realm = realmSource;
                m_minion.Model = 881;
                //spawnpoint
                m_minion.X = 542193 + m_rnd.Next(40);
                m_minion.Y = 550173 + m_rnd.Next(40);                
            }
            else if (realmSource == eRealm.Albion)
            {
                m_minion.GuildName = "Albion";
                m_minion.Realm = realmSource;
                m_minion.Model = 882;
                //spawnpoint
                m_minion.X = 572108 + m_rnd.Next(40);
                m_minion.Y = 550063 + m_rnd.Next(40);
            }
            else if (realmSource == eRealm.Midgard)
            {
                m_minion.GuildName = "Midgard";
                m_minion.Realm = realmSource;
                m_minion.Model = 883;
                //spawnpoint
                m_minion.X = 557070 + m_rnd.Next(40);
                m_minion.Y = 572491 + m_rnd.Next(40);
            }
            else
            {
                return;
            }

            //finally add mob to world
            m_minion.AddToWorld();

            //send mob to attack enemy nexus (not working becasue of brain: mob turns around)
            if (realmTarget == eRealm.Albion)
            {
                m_minion.WalkTo(572108, 550063, 8640, 400);
            }
            else if (realmTarget == eRealm.Midgard)
            {
                m_minion.WalkTo(557070, 572491, 8640, 400);
            }
            else if (realmTarget == eRealm.Hibernia)
            {
                m_minion.WalkTo(542193, 550173, 8640, 400);
            }
        }

        //This function is called whenever the event is stopped
        //It should be used to clean up!
        [ScriptUnloadedEvent]
        public static void OnScriptUnload(DOLEvent e, object sender, EventArgs args)
        {
            //We stop our mob wave timer
            if (m_waveTimer != null)
            {
                m_waveTimer.Stop();
                m_waveTimer.Close();
            }

            //We stop our mob solo timer just in case it is on
            if (m_soloTimer != null)
            {
                m_soloTimer.Stop();
                m_soloTimer.Close();
            }

            //REMOVE EVERYTHING! FULL RESET! ANYTHING CREATED MUST BE DESTROYED!

            //We delete our master from the world
            if (m_gameMaster != null)
                m_gameMaster.Delete();
        }
    }
}