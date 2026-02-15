using Mirror;

namespace Game.Core.Items
{
    public interface ItemArgument
    {
        public string Name { get; }
        public object Value { get; }
    }

    public struct IntItemArgument : ItemArgument
    {
        public readonly string Name => name;
        public readonly object Value => value;

        public string name;
        public int value;

        public IntItemArgument(string name, int value)
        {
            this.name = name;
            this.value = value;
        }
    }

    public static class ItemArgumentUtility
    {
        public static ItemArgument ReadArgument(this NetworkReader reader)
        {
            var type = reader.ReadByte();
            return type switch
            {
                0 => new IntItemArgument(reader.ReadString(), reader.ReadInt()),
                _ => throw new($"Invalid type {type}. This type cannot be read")
            };
        }

        public static void WriteArgument(this NetworkWriter writer, ItemArgument argument)
        {
            if (argument is IntItemArgument intArg)
            {
                writer.WriteByte(0);
                writer.WriteString(intArg.name);
                writer.WriteInt(intArg.value);
            }
            else throw new($"Argument of type {argument.GetType()} cannot be written");
        }

        public static T ParseArgument<T>(this ItemArgument[] arguments, string name, T defaultValue)
        {
            foreach (var arg in arguments)
            {
                if (arg.Name != name) continue;
                return (T)arg.Value;
            }

            return defaultValue;
        }
    }
}