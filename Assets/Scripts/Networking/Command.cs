using System.Collections.Generic;

namespace Tanki.Networking
{
    public class Command
    {
        public string Type { get; private set; }
        public List<string> Arguments { get; private set; }
        public string RawContent { get; private set; }

        public Command(string type, List<string> arguments, string rawContent)
        {
            Type = type;
            Arguments = arguments;
            RawContent = rawContent;
        }

        public static List<Command> Parse(string rawData)
        {
            var commands = new List<Command>();
            var rawCommands = rawData.Split(new[] { ProtocolConstants.CommandDelimiter }, System.StringSplitOptions.RemoveEmptyEntries);

            foreach (var rawCmd in rawCommands)
            {
                var parts = rawCmd.Split(new[] { ProtocolConstants.ArgumentDelimiter }, System.StringSplitOptions.None);
                if (parts.Length > 0)
                {
                    var type = parts[0];
                    var arguments = new List<string>();
                    for (int i = 1; i < parts.Length; i++)
                    {
                        arguments.Add(parts[i]);
                    }
                    commands.Add(new Command(type, arguments, rawCmd));
                }
            }

            return commands;
        }
    }
}
