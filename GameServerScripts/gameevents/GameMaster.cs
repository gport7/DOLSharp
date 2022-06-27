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
 * Author:	SmallHorse & Crystalö
 * Date:	20.11.2003
 * This script should be put in /scripts/gameevents directory.
 * This event simulates a guard-trainer and some of his trainees.
 * They all come with basic equipment and do some attack as well
 * as some defense styles. Note: This is just a fight-simulation,
 * no real combat, hp-loss or anything, just the clanging of swords.
 */
using System;
using System.Reflection;
using System.Timers;
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

            //If someone interacts (rightclicks on the game master this function is called)
            public override bool Interact(GamePlayer player)
            {
                if (!base.Interact(player))
                    return false;
                Say("Can't you see I am busy " + player.Name + ", please be quiet!");
                return true;
            }
        }

        //Declare all the variables that will be used in the scope
        //of this event

        //We have one GameMasterNPC who will control the game sessions
        private static GameMasterNPC m_gameMaster;
        //We need some randomness
        private static Random m_rnd;
        //And we need a timer to time our styles
        private static Timer m_fightTimer;


        //This function will be called to initialize the event
        //It needs to be declared in EVERY game-event and is used
        //to start things up.
        [ScriptLoadedEvent]
        public static void OnScriptsCompiled(DOLEvent e, object sender, EventArgs args)
        {
            //We set a new random class
            m_rnd = new Random();

            //We create our game master
            m_gameMaster = new GameMasterNPC();
            m_gameMaster.X = 42079;
            m_gameMaster.Y = 38399;
            m_gameMaster.Z = 10341;
            m_gameMaster.Name = "Game Master";

            //Now we try to add the master to the world
            bool good = true;
            if (!m_gameMaster.AddToWorld())
                good = false;

            //We start our fight timer that will make the guards
            //act each second
            m_fightTimer = new Timer(1000);
            m_fightTimer.AutoReset = true;
            m_fightTimer.Elapsed += new ElapsedEventHandler(MakeAttackSequence);
            m_fightTimer.Start();

            if (log.IsInfoEnabled)
                if (log.IsInfoEnabled)
                    log.Info("GameMasterNPCEvent initialized: " + good);
        }

        //This function is a timercallback function that 
        //is called every second
        protected static void MakeAttackSequence(object sender, ElapsedEventArgs args)
        {
            GameObject minionMelee;
            GameObject minionCaster;

            //We randomly set an attacker
            int currentAttacker = m_rnd.Next(8);
            m_gameMaster.Say("1 second, bro!");


        }

        //This function is called whenever the event is stopped
        //It should be used to clean up!
        [ScriptUnloadedEvent]
        public static void OnScriptUnload(DOLEvent e, object sender, EventArgs args)
        {
            //We stop our timer ... no more attacks
            if (m_fightTimer != null)
            {
                m_fightTimer.Stop();
                m_fightTimer.Close();
            }

            //We delete our master from the world
            if (m_gameMaster != null)
                m_gameMaster.Delete();
        }
    }
}