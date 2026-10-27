using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using static Paname.PanameArt;

namespace Paname
{
    // Online co-op without a game server. The host sequences every player's input into 60 Hz
    // frames and every browser runs the same simulation from them: no game state is sent, so
    // new heroes, enemies or bosses work online as long as TickSimulation stays deterministic.
    public sealed partial class PanameGame
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern string ALNetInit(string objectName);
        [DllImport("__Internal")] static extern void ALNetSend(string target,string message);
        [DllImport("__Internal")] static extern void ALNetStatus(string json);
#else
        static string ALNetInit(string objectName)=>"{\"mode\":\"local\"}";
        static void ALNetSend(string target,string message){}
        static void ALNetStatus(string json){}
#endif
        const float NetStep=1f/60;
        const int HashInterval=60;
        [Serializable] sealed class NetConfig {public string mode,code,name,self;}
        [Serializable] sealed class NetLobby {public string build;public int mission,difficulty,score;public bool[] joined=new bool[4];public int[] heroes=new int[4];public string[] peers=new string[4],names=new string[4];}
        struct NetInput {public int move,flags,jumps,specials,grenades,usedJumps,usedSpecials,usedGrenades;}
        sealed class NetFrame {public int left;public readonly int[] moves=new int[4],flags=new int[4];}
        public bool Online {get;private set;}
        public bool IsHost {get;private set;}
        public int LocalSlotIndex {get;private set;}=-1;
        public int NetTick {get;private set;}
        public bool NetDesync {get;private set;}
        public string NetCode=>netCode;
        string netCode="",netSelf="",netName="",netHost="",netStatus="",netError="";
        readonly string[] playerNames=new string[4],slotPeers=new string[4];
        readonly NetInput[] netInputs=new NetInput[4];
        readonly Queue<NetFrame> netFrames=new Queue<NetFrame>();
        readonly Dictionary<int,int> hostHashes=new Dictionary<int,int>();
        readonly LocalSlot netLocal=new LocalSlot{joined=true};
        readonly StringBuilder netOut=new StringBuilder();
        bool netBypass;
        float netClock,netSendClock,netWaitClock;
        int netLeftMask,localJumps,localSpecials,localGrenades,lastHash;
        Phase reportedPhase;
        string lastInputSent="";
        static string BuildId=>Application.version+"/"+Application.buildGUID;
        static int Mod(int value,int count)=>(value%count+count)%count;
        static int Quantize(float move)=>Mathf.RoundToInt(Mathf.Clamp(move,-1,1)*100);

        void Start()
        {
            var config=JsonUtility.FromJson<NetConfig>(ALNetInit(gameObject.name));
            if(config==null||(config.mode!="host"&&config.mode!="guest"))return;
            Online=true;IsHost=config.mode=="host";netCode=config.code??"";netSelf=config.self??"";netName=CleanName(config.name);
            for(int i=0;i<4;i++){slots[i].joined=false;slots[i].pad=null;slotPeers[i]=null;playerNames[i]=null;}
            if(IsHost){slots[0].joined=true;slotPeers[0]=netSelf;playerNames[0]=netName;LocalSlotIndex=0;}
            else {netStatus="CONNEXION À LA PARTIE...";NetSendAll(Hello());}
            BuildLobbyActors();ReportStatus();
        }
        string Hello()=>"J"+BuildId+"\t"+netName;
        static string CleanName(string name)
        {
            var clean=new StringBuilder();
            foreach(char c in (name??"").Trim())if(char.IsLetterOrDigit(c)||c==' '||c=='-'||c=='_')clean.Append(c);
            string result=clean.ToString().Trim();
            return result.Length==0?"JOUEUR":result.Length>12?result.Substring(0,12):result;
        }
        public string PlayerTag(int slot)=>Online&&!string.IsNullOrEmpty(playerNames[slot])?playerNames[slot].ToUpperInvariant():"J"+(slot+1);
        void NetSend(string peer,string message){if(!string.IsNullOrEmpty(peer))ALNetSend(peer,message);}
        void NetSendAll(string message)=>ALNetSend("",message);

        // Called from the page through SendMessage: "kind\npeer\ndata".
        public void OnNetEvent(string message)
        {
            if(!Online)return;
            int a=message.IndexOf('\n'),b=a<0?-1:message.IndexOf('\n',a+1);if(b<0)return;
            string kind=message.Substring(0,a),peer=message.Substring(a+1,b-a-1),data=message.Substring(b+1);
            if(kind=="join"){if(!IsHost&&netHost.Length==0)NetSend(peer,Hello());}
            else if(kind=="leave")PeerLeft(peer);
            else if(kind=="error"){if(!IsHost&&netHost.Length==0)netStatus="CONNEXION DIRECTE IMPOSSIBLE. RÉESSAIE OU CHANGE DE RÉSEAU.";}
            else if(kind=="msg"&&data.Length>0){if(IsHost)HostReceive(peer,data[0],data.Substring(1));else GuestReceive(peer,data[0],data.Substring(1));}
        }

        // ---------- Host ----------
        void HostReceive(string peer,char type,string body)
        {
            int slot=Array.IndexOf(slotPeers,peer);
            if(type=='J')
            {
                string[] parts=body.Split('\t');
                if(parts[0]!=BuildId){NetSend(peer,"RVERSION");return;}
                if(slot<0)
                {
                    if(phase!=Phase.Lobby){NetSend(peer,"RSTARTED");return;}
                    slot=Array.FindIndex(slots,s=>!s.joined);
                    if(slot<0){NetSend(peer,"RFULL");return;}
                    slots[slot].joined=true;slots[slot].pad=null;slots[slot].hero=FreeHero(slot);slotPeers[slot]=peer;netInputs[slot]=default;
                    BuildLobbyActors();
                }
                playerNames[slot]=CleanName(parts.Length>1?parts[1]:"");
                HostSendLobby();
            }
            else if(slot<=0)return;
            else if(type=='P'&&phase==Phase.Lobby&&int.TryParse(body,out int hero)){slots[slot].hero=Mod(hero,HeroCount);BuildLobbyActors();HostSendLobby();}
            else if(type=='I')ParseInput(body,ref netInputs[slot]);
            else if(type=='H')CheckHash(body);
        }
        int FreeHero(int slot)
        {
            for(int hero=0;hero<HeroCount;hero++)
            {bool taken=false;for(int i=0;i<4;i++)if(i!=slot&&slots[i].joined&&slots[i].hero==hero)taken=true;if(!taken)return hero;}
            return Mod(slot,HeroCount);
        }
        static void ParseInput(string body,ref NetInput input)
        {
            string[] v=body.Split(',');
            if(v.Length<5||!int.TryParse(v[0],out int move)||!int.TryParse(v[1],out int flags)||!int.TryParse(v[2],out int jumps)||!int.TryParse(v[3],out int specials)||!int.TryParse(v[4],out int grenades))return;
            input.move=Mathf.Clamp(move,-100,100);input.flags=flags&HeldMask;input.jumps=jumps;input.specials=specials;input.grenades=grenades;
        }
        string LobbyJson()
        {
            var lobby=new NetLobby{build=BuildId,mission=missionIndex,difficulty=SelectedDifficulty,score=carriedScore};
            for(int i=0;i<4;i++){lobby.joined[i]=slots[i].joined;lobby.heroes[i]=slots[i].hero;lobby.peers[i]=slotPeers[i]??"";lobby.names[i]=playerNames[i]??"";}
            return JsonUtility.ToJson(lobby);
        }
        void HostSendLobby(){if(Online&&IsHost){NetSendAll("L"+LobbyJson());ReportStatus();}}
        // Lobby settings only the host may change; guests' clicks are ignored.
        bool HostOnly(Action action)
        {
            if(!Online||netBypass)return false;
            if(IsHost){netBypass=true;action();netBypass=false;HostSendLobby();}
            return true;
        }
        bool OnlineHero(int hero)
        {
            if(!Online||netBypass)return false;
            if(IsHost){netBypass=true;SelectHero(0,hero);netBypass=false;HostSendLobby();}
            else NetSend(netHost,"P"+hero);
            return true;
        }
        bool OnlineRound()
        {
            if(!Online||netBypass)return false;
            if(IsHost&&phase!=Phase.Playing)
            {
                NetSendAll("S"+LobbyJson());
                ResetLockstep();netBypass=true;BeginRound();netBypass=false;ReportStatus();
            }
            return true;
        }
        bool OnlineReturn()
        {
            if(!Online||netBypass)return false;
            if(IsHost){NetSendAll("B");netBypass=true;ReturnToLobby();netBypass=false;HostSendLobby();}
            return true;
        }
        void ResetLockstep()
        {
            netFrames.Clear();hostHashes.Clear();NetTick=0;netClock=0;netWaitClock=0;netLeftMask=0;NetDesync=false;
            for(int i=0;i<4;i++)netInputs[i]=default;
            localJumps=0;localSpecials=0;localGrenades=0;lastInputSent="";
        }
        void HostStep(PlayerCommand local)
        {
            netInputs[0].move=Quantize(local.move);netInputs[0].flags=HeldFlags(local);netInputs[0].jumps=localJumps;netInputs[0].specials=localSpecials;netInputs[0].grenades=localGrenades;
            netClock=Mathf.Min(netClock+Time.unscaledDeltaTime,.25f);netOut.Clear();
            while(netClock>=NetStep&&phase==Phase.Playing)
            {
                netClock-=NetStep;
                var frame=new NetFrame{left=netLeftMask};netLeftMask=0;
                for(int i=0;i<4;i++)
                {
                    if(!slots[i].joined)continue;
                    ref var input=ref netInputs[i];int flags=input.flags;
                    if(input.jumps!=input.usedJumps){flags|=4;input.usedJumps=input.jumps;}
                    if(input.specials!=input.usedSpecials){flags|=8;input.usedSpecials=input.specials;}
                    if(input.grenades!=input.usedGrenades){flags|=32;input.usedGrenades=input.grenades;}
                    frame.moves[i]=input.move;frame.flags[i]=flags;
                }
                if(netOut.Length>0)netOut.Append('/');
                netOut.Append(frame.left);for(int i=0;i<4;i++)netOut.Append(' ').Append(frame.moves[i]).Append(' ').Append(frame.flags[i]);
                RunFrame(frame);
            }
            if(netOut.Length>0)NetSendAll("F"+netOut);
        }
        void CheckHash(string body)
        {
            string[] v=body.Split(',');
            if(v.Length<2||!int.TryParse(v[0],out int tick)||!int.TryParse(v[1],out int hash))return;
            if(hostHashes.TryGetValue(tick,out int expected)&&expected!=hash&&!NetDesync){NetDesync=true;NetSendAll("D"+tick);Debug.LogWarning("Désynchronisation au pas "+tick);}
        }
        void PeerLeft(string peer)
        {
            if(!IsHost){if(peer==netHost){netError="L'HÔTE A QUITTÉ LA PARTIE.";ReportStatus();}return;}
            int slot=Array.IndexOf(slotPeers,peer);if(slot<=0)return;
            netInputs[slot]=default;
            // During a mission the departure is applied at the same tick in every browser.
            if(phase==Phase.Playing){slotPeers[slot]=null;netLeftMask|=1<<slot;return;}
            DropPlayer(slot);if(phase==Phase.Lobby){BuildLobbyActors();HostSendLobby();}
        }
        void DropPlayer(int slot)
        {
            slots[slot].joined=false;slotPeers[slot]=null;playerNames[slot]=null;
            if(players[slot]!=null){players[slot].root.gameObject.SetActive(false);Destroy(players[slot].root.gameObject);players[slot]=null;}
        }

        // ---------- Guest ----------
        void GuestReceive(string peer,char type,string body)
        {
            if(netHost.Length==0){if(type!='L'&&type!='R')return;netHost=peer;}
            else if(peer!=netHost)return;
            if(type=='L')ApplyLobby(body,false);
            else if(type=='S')ApplyLobby(body,true);
            else if(type=='B'){netFrames.Clear();netBypass=true;ReturnToLobby();netBypass=false;}
            else if(type=='F')ParseFrames(body);
            else if(type=='D')NetDesync=true;
            else if(type=='R')netError=body=="FULL"?"LA PARTIE EST COMPLÈTE : 4 JOUEURS MAXIMUM.":body=="STARTED"?"LA MISSION A DÉJÀ COMMENCÉ. RECHARGE LA PAGE QUAND L'ÉQUIPE SERA REVENUE AU MENU.":"VERSIONS DU JEU DIFFÉRENTES : RECHARGEZ TOUS LA PAGE.";
            if(type=='R')ReportStatus();
        }
        void ApplyLobby(string body,bool start)
        {
            NetLobby lobby;try{lobby=JsonUtility.FromJson<NetLobby>(body);}catch(ArgumentException){return;}
            if(lobby==null||lobby.joined==null||lobby.joined.Length<4)return;
            netBypass=true;netStatus="";
            if(phase!=Phase.Lobby&&!start)ReturnToLobby();
            // Guests follow the host's mission even when they have not unlocked it, and even from the
            // victory screen when the host chains into the next mission.
            if(lobby.mission!=missionIndex&&(phase==Phase.Lobby||start))LoadMission(lobby.mission);
            if(start)carriedScore=lobby.score;
            SelectedDifficulty=Mathf.Clamp(lobby.difficulty,0,DifficultyCount-1);
            LocalSlotIndex=-1;
            for(int i=0;i<4;i++)
            {
                slots[i].joined=lobby.joined[i];slots[i].pad=null;slots[i].hero=Mod(lobby.heroes[i],HeroCount);
                slotPeers[i]=lobby.peers[i];playerNames[i]=lobby.names[i];
                if(lobby.joined[i]&&lobby.peers[i]==netSelf)LocalSlotIndex=i;
            }
            if(start){ResetLockstep();BeginRound();}
            else if(phase==Phase.Lobby)BuildLobbyActors();
            netBypass=false;ReportStatus();
        }
        void ParseFrames(string body)
        {
            foreach(string chunk in body.Split('/'))
            {
                string[] v=chunk.Split(' ');if(v.Length<9)continue;
                var frame=new NetFrame();int.TryParse(v[0],out frame.left);
                for(int i=0;i<4;i++){int.TryParse(v[1+i*2],out frame.moves[i]);int.TryParse(v[2+i*2],out frame.flags[i]);}
                netFrames.Enqueue(frame);
            }
        }
        // Held buttons as flags, pressed ones as counters, so a press survives a lost or merged message.
        string GuestInput(PlayerCommand local)=>Quantize(local.move)+","+HeldFlags(local)+","+localJumps+","+localSpecials+","+localGrenades;
        void GuestStep(PlayerCommand local)
        {
            string input=GuestInput(local);
            netSendClock-=Time.unscaledDeltaTime;
            if(input!=lastInputSent||netSendClock<=0){NetSend(netHost,"I"+input);lastInputSent=input;netSendClock=.2f;}
            // Play frames at 60 Hz, and catch up when a hitch let the buffer grow.
            netClock+=Time.unscaledDeltaTime;
            int budget=Mathf.FloorToInt(netClock/NetStep);
            if(netFrames.Count>6)budget=Mathf.Max(budget,netFrames.Count-3);
            int run=Mathf.Min(Mathf.Min(budget,netFrames.Count),20);
            for(int i=0;i<run;i++)RunFrame(netFrames.Dequeue());
            netClock=Mathf.Clamp(netClock-run*NetStep,0,NetStep*2);
            netWaitClock=run==0?netWaitClock+Time.unscaledDeltaTime:0;
        }

        // ---------- Shared ----------
        bool OnlineUpdate()
        {
            if(!Online)return false;
            netLocal.pad=Gamepad.current;
            var local=LocalControls.Read(netLocal,0);
            if(local.jump)localJumps++;if(local.special)localSpecials++;if(local.grenade)localGrenades++;
            if(phase==Phase.Lobby)OnlineLobbyInput();
            else if(phase==Phase.Playing){if(IsHost)HostStep(local);else GuestStep(local);}
            else if(IsHost&&(phase==Phase.Won||phase==Phase.Lost)&&Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame)ReturnToLobby();
            else if(IsHost&&phase==Phase.Won&&NextMissionDefinition!=null&&ContinuePressed())NextMission();
            if(phase!=reportedPhase)ReportStatus();
            UpdateCamera(Time.unscaledDeltaTime);UpdateSoundtrack(Time.unscaledDeltaTime);
            return true;
        }
        void OnlineLobbyInput()
        {
            var k=Keyboard.current;var pad=netLocal.pad;int direction=0;
            if(k!=null&&!LobbyKeysBlocked){if(k.qKey.wasPressedThisFrame||k.aKey.wasPressedThisFrame||k.leftArrowKey.wasPressedThisFrame)direction=-1;if(k.eKey.wasPressedThisFrame||k.dKey.wasPressedThisFrame||k.rightArrowKey.wasPressedThisFrame)direction=1;}
            if(pad!=null){if(pad.dpad.left.wasPressedThisFrame)direction=-1;if(pad.dpad.right.wasPressedThisFrame)direction=1;}
            if(direction!=0&&LocalSlotIndex>=0)OnlineHero(Mod(slots[LocalSlotIndex].hero+direction,HeroCount));
            if(!IsHost||LobbyKeysBlocked)return;
            if((k!=null&&k.enterKey.wasPressedThisFrame)||(pad!=null&&pad.startButton.wasPressedThisFrame))BeginRound();
            if(pad!=null&&pad.leftShoulder.wasPressedThisFrame)SelectMission(-1);
            if(pad!=null&&pad.rightShoulder.wasPressedThisFrame)SelectMission(1);
        }
        void RunFrame(NetFrame frame)
        {
            if(frame.left!=0)for(int i=0;i<4;i++)if((frame.left&(1<<i))!=0)DropPlayer(i);
            for(int i=0;i<4;i++)frameInputs[i]=Command(frame.moves[i],frame.flags[i]&FlagMask);
            RecordTick(frame.left,frame.moves,frame.flags);TickSimulation(NetStep,frameInputs);EndReplayTick();NetTick++;
            if(NetTick%HashInterval!=0)return;
            lastHash=StateHash();
            if(IsHost){hostHashes[NetTick]=lastHash;hostHashes.Remove(NetTick-HashInterval*20);}
            else NetSend(netHost,"H"+NetTick+","+lastHash);
            ReportStatus();
        }
        // Compared between browsers to detect any simulation drift.
        int StateHash()
        {
            int h=-2128831035;
            void Mix(int v){h=unchecked((h^v)*16777619);}
            void MixF(float v)=>Mix(BitConverter.SingleToInt32Bits(v));
            Mix(Score);Mix(Wave);Mix(Reserves);Mix(kills);Mix(enemies.Count);Mix(shots.Count);Mix(bossHealth);Mix((int)phase);
            foreach(var p in players)if(p!=null){MixF(p.position.x);MixF(p.position.y);Mix(p.hp);Mix(p.weapon.hash);Mix(p.ammo);Mix(p.grenades);Mix(p.rescued);}
            Mix(thrown.Count);Mix(HostagesFreed);foreach(var o in hostages){Mix(o.state);MixF(o.position.x);}
            foreach(var e in enemies){MixF(e.position.x);Mix(e.hp);}
            return h;
        }
        void ReportStatus()
        {
            reportedPhase=phase;
            var json=new StringBuilder("{\"role\":\"").Append(!Online?"local":IsHost?"host":"guest").Append("\",\"slot\":").Append(LocalSlotIndex)
                .Append(",\"phase\":\"").Append(phase).Append("\",\"tick\":").Append(NetTick).Append(",\"hash\":").Append(lastHash)
                .Append(",\"desync\":").Append(NetDesync?"true":"false").Append(",\"error\":\"").Append(netError).Append('"').Append(",\"players\":[");
            bool first=true;
            for(int i=0;i<4;i++)if(slots[i].joined){if(!first)json.Append(',');first=false;json.Append('"').Append(PlayerTag(i)).Append('"');}
            ALNetStatus(json.Append("]}").ToString());
        }
        void LeaveOnline()
        {
            Online=false;IsHost=false;netError="";netStatus="";netHost="";LocalSlotIndex=-1;
            for(int i=0;i<4;i++){slots[i].joined=i==0;slots[i].pad=null;slotPeers[i]=null;playerNames[i]=null;}
            phase=Phase.Lobby;cameraX=CurrentMission.cameraMin;BuildLobbyActors();ReportStatus();
        }

        // ---------- Interface ----------
        // Replaces the local lobby footer while online. Returns false in local play.
        bool DrawOnlineLobbyFooter()
        {
            if(!Online)return false;
            Label(51,633,860,22,"PARTIE EN LIGNE  ·  CODE "+netCode,13,Cream,true);
            if(!IsHost)Label(51,659,860,20,"L'hôte lance la partie.",12,Gold,true);
            if(IsHost){if(Button(932,609,296,59,"DÉPLOYER L'ÉQUIPE  »",Coral,true))BeginRound();}
            else{Panel(932,609,296,59,C("304C56"));Label(932,609,296,59,"EN ATTENTE DE L'HÔTE...",16,Cream,true,TextAnchor.MiddleCenter);}
            return true;
        }
        string SlotLabel(int slot)=>Online?PlayerTag(slot)+(slot==0?"  (HÔTE)":slot==LocalSlotIndex?"  (TOI)":""):"J"+(slot+1)+"  "+slots[slot].DeviceName;
        // Connection screens replace the whole interface so nothing underneath stays clickable.
        bool DrawOnlineBlocker()
        {
            string message=!Online?"":netError.Length>0?netError:netStatus;
            if(message.Length==0)return false;
            Panel(-2000,-2000,5280,4720,new Color(.06f,.12f,.16f,.96f));
            Label(140,250,1000,90,message,26,Cream,true,TextAnchor.MiddleCenter);
            if(netError.Length==0)Label(140,340,1000,30,"Code de la partie : "+netCode+"  ·  l'hôte doit garder son onglet ouvert",14,Gold,true,TextAnchor.MiddleCenter);
            else if(Button(440,393,400,58,"JOUER EN LOCAL",Coral,true))LeaveOnline();
            return true;
        }
        void DrawOnlineBanner()
        {
            if(!Online)return;
            if(NetDesync){Panel(335,224,610,30,new Color(.45f,.1f,.08f,.92f));Label(345,224,590,30,"DÉSYNCHRONISATION DÉTECTÉE ENTRE LES JOUEURS",14,Cream,true,TextAnchor.MiddleCenter);}
            else if(!IsHost&&phase==Phase.Playing&&netWaitClock>1.5f){Panel(335,224,610,30,new Color(.08f,.16f,.2f,.92f));Label(345,224,590,30,"EN ATTENTE DE L'HÔTE...",14,Gold,true,TextAnchor.MiddleCenter);}
        }
    }
}
