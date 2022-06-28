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
                                    "[Leave Queue]\n");
                return true;
            }

            public override bool WhisperReceive(GameLiving source, string str)
            {
                if (!base.WhisperReceive(source, str)) return false;
                if (!(source is GamePlayer)) return false;

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

                        //ELSE
                        //SendReply(player, "You can't leave a queue that you aren't in.");
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
                //move players to BG
                //m_waveTimer.Start();
            }

            //cleaner than SayTo
            public void SendReply(GamePlayer player, string msg)
            {
                player.Out.SendMessage(msg, eChatType.CT_System, eChatLoc.CL_PopupWindow);
            }
        }
        #endregion    

        #region variables
        //some randomness just in case (not implemented anywhere) refer to fightingmobs
        private static Random m_rnd;
        //one GameMasterNPC who will control the game sessions
        private static GameMasterNPC m_gameMaster;
        //timers to time the creation of our mob waves
        private static Timer m_waveTimer;
        private static Timer m_soloTimer;
        //two ints for mobs per wave
        private static int i = 0; //counter start at 0
        private static int waveSize = 10; //max mobs per wave
        //2d array for players (player and realm(to be changed to proper realm on start))
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
            //We set a new random class
            m_rnd = new Random();

            //create our game master
            m_gameMaster = new GameMasterNPC();
            m_gameMaster.X = 42079;
            m_gameMaster.Y = 38399;
            m_gameMaster.Z = 10341;
            m_gameMaster.Name = "Game Master";

            //try to add the master to the world
            bool good = true;
            if (!m_gameMaster.AddToWorld())
                good = false;

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
            m_gameMaster.Say("Mob wave released!"); //dummy action for testing

            //timer for one mob every x milliseconds
            m_soloTimer = new Timer(1000);
            m_soloTimer.AutoReset = true;
            m_soloTimer.Elapsed += new ElapsedEventHandler(CreateMobSolo);
            m_soloTimer.Start();
        }

        protected static void CreateMobSolo(object sender, ElapsedEventArgs args)
        {
            i++; //increases by 1 per timer elapse
            m_gameMaster.Say("Mob " + i + " created!");  //dummy action for testing

            //when i meets the limit the timer stops, and no more mobs are made in this wave
            if (i >= waveSize)
                if (m_soloTimer != null)
                {
                    i = 0;//reset i to 0
                    m_soloTimer.Stop();
                    m_soloTimer.Close();
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

            //We remove all minions/casters from the DB by mob name
            //TBD

            //We delete our master from the world
            if (m_gameMaster != null)
                m_gameMaster.Delete();
        }
    }
}