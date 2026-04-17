using System;
using System.Reflection;
using System.Linq;

class InspectConstructors
{
    static void Main()
    {
        var assembly = Assembly.LoadFrom(@"packages\Albion.Network.5.0.1\lib\netstandard2.0\Albion.Network.dll");

        Console.WriteLine("=== EventPacket Constructors ===");
        var eventPacketType = assembly.GetType("Albion.Network.EventPacket");
        if (eventPacketType != null)
        {
            var constructors = eventPacketType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var ctor in constructors)
            {
                var parameters = ctor.GetParameters();
                Console.WriteLine($"{ctor.IsPublic ? "public" : "private"} EventPacket({string.Join(", ", parameters.Select(p => $"{p.ParameterType.Name} {p.Name}"))})");
            }
        }

        Console.WriteLine("\n=== RequestPacket Constructors ===");
        var requestPacketType = assembly.GetType("Albion.Network.RequestPacket");
        if (requestPacketType != null)
        {
            var constructors = requestPacketType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var ctor in constructors)
            {
                var parameters = ctor.GetParameters();
                Console.WriteLine($"{ctor.IsPublic ? "public" : "private"} RequestPacket({string.Join(", ", parameters.Select(p => $"{p.ParameterType.Name} {p.Name}"))})");
            }
        }

        Console.WriteLine("\n=== ResponsePacket Constructors ===");
        var responsePacketType = assembly.GetType("Albion.Network.ResponsePacket");
        if (responsePacketType != null)
        {
            var constructors = responsePacketType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var ctor in constructors)
            {
                var parameters = ctor.GetParameters();
                Console.WriteLine($"{ctor.IsPublic ? "public" : "private"} ResponsePacket({string.Join(", ", parameters.Select(p => $"{p.ParameterType.Name} {p.Name}"))})");
            }
        }

        Console.WriteLine("\n=== EventPacket Properties/Fields ===");
        if (eventPacketType != null)
        {
            Console.WriteLine("Properties:");
            foreach (var prop in eventPacketType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                Console.WriteLine($"  {prop.PropertyType.Name} {prop.Name} {{ get: {prop.CanRead}, set: {prop.CanWrite} }}");
            }
            Console.WriteLine("Fields:");
            foreach (var field in eventPacketType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                Console.WriteLine($"  {field.FieldType.Name} {field.Name}");
            }
        }
    }
}
