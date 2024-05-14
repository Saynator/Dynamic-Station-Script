using Newtonsoft.Json;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        // This file contains your actual script.
        //
        // You can either keep all your code here, or you can create separate
        // code files to make your program easier to navigate while coding.
        //
        // In order to add a new utility class, right-click on your project, 
        // select 'New' then 'Add Item...'. Now find the 'Space Engineers'
        // category under 'Visual C# Items' on the left hand side, and select
        // 'Utility Class' in the main area. Name it in the box below, and
        // press OK. This utility class will be merged in with your code when
        // deploying your final script.
        //
        // You can also simply create a new utility class manually, you don't
        // have to use the template if you don't want to. Just do so the first
        // time to see what a utility class looks like.
        // 
        // Go to:
        // https://github.com/malware-dev/MDK-SE/wiki/Quick-Introduction-to-Space-Engineers-Ingame-Scripts
        //
        // to learn more about ingame scripts.
        public class StationPacket
        {
            public static readonly string ScriptTag = "Manager";
            public long PBID { get; set; }
            public Instruction Instruction { get; set; }
            public static bool SendPacket(StationPacket packet, IMyProgrammableBlock scriptManager)
            {
                string instructionData = "";
                switch (packet.Instruction.Type)
                {
                    case "Update":
                        instructionData = packet.Instruction.Data2.Count().ToString();
                        break;
                    case "GraphingRequest":
                        foreach (var item in packet.Instruction.Data1)
                        {
                            instructionData += $"{item.SubtypeId};{item.TypeId} ";
                        }
                        break;
                    case "Echo":
                        instructionData = packet.Instruction.DataString;
                        break;
                    case "Return":
                        instructionData = packet.Instruction.DataString;
                        break; //TODO: Finish
                }
                string instructionType = packet.Instruction.Type;
                string arg = $"-isCommand {StationPacket.ScriptTag} {packet.PBID} {instructionType} {instructionData}";
                return scriptManager.TryRun(arg);
            }
        }
        private Dictionary<string, Action> _commands = new Dictionary<string, Action>(StringComparer.OrdinalIgnoreCase);
        private readonly MyCommandLine _commandLine = new MyCommandLine();
        public class Instruction
        {
            public string Type { get; set; }
            public List<MyItemType> Data1 { get; set; }
            public List<IMyInventoryOwner> Data2 { get; set; }
            public string DataString { get; set; }
        }



        UpdateType updateSource;
        bool _mainScriptManager = true;
        public Program()
        {
            _commands["Update"] = Update;
            _commands["Echo"] = LocalEcho;
            _commands["Request"] = Request;
            _commands["Command"] = Command;
            _commands["Return"] = Return;
            _commands["GraphingRequest"] = Display;
            Me.CustomName = "$SYS Manager";
            Load();
        }

        private void Return()
        {
            
        }

        private void Command()
        {
            
        }

        private void Request()
        {
            IMyProgrammableBlock pb = GridTerminalSystem.GetBlockWithId(long.Parse(_commandLine.Argument(1))) as IMyProgrammableBlock;
            if (updateSource == UpdateType.Script)
            {
                Runtime.UpdateFrequency |= UpdateFrequency.Once;
                Echo($"{pb.Name} issued a request");
                return;
            }
            Echo("Return:" + _commandLine.Argument(3));
            StationPacket.SendPacket(new StationPacket
            {
                PBID = Me.EntityId,
                Instruction = new Instruction
                {
                    Type = "Return",
                }
            }, pb);
        }

        private void LocalEcho()
        {
            Echo($"Incoming Echo from {_commandLine.Argument(1)}({_commandLine.Argument(0)}):\n{_commandLine.Argument(4)}");
        }

        public void Save()
        {
            Storage = "";
        }

        public void Main(string argument, UpdateType updateSource)
        {
            this.updateSource = updateSource;
            if (_commandLine.TryParse(argument)){
                if (!_commandLine.Switch("isCommand")) { Echo("This ain't a command bruv");  return; }
                Action action;
                string command = _commandLine.Argument(2);
                if (command == null)
                {
                    Echo("No command specified");
                }
                else if (_commands.TryGetValue(command, out action))
                { 
                    action();
                }
                else
                {
                    Echo($"Unknown command {command}");
                }
                _commandLine.Clear();
            }
        }
        private void Display()
        {
        }
        public void Load() { }
        public void Update() { }
        public void Settings()
        {

        }
        public void AddScript(long id)
        {
            GridTerminalSystem.GetBlockWithId(id);
        }
    }
}
