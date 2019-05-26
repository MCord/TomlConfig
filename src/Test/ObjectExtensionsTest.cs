namespace Test
{
    using System.Collections.Generic;
    using NFluent;
    using TomlConfiguration;
    using Xunit;

    public class ObjectExtensionsTest
    {
        class Car
        {
            public int Wheels { get; set; }
            public Motor Motor { get; set; }
            public List<Door> Doors { get; set; }
        }

        class Door
        {
            public bool IsOpen { get; set; }
        }

        class Motor
        {
            public GearBox GearBox { get; set; }
        }

        class GearBox
        {
            public string Type { get; set; }
        }


        [Fact]
        public void ShouldGetNestedValue()
        {
            var instance = new Car
            {
                Wheels = 4,
                Motor = new Motor
                {
                    GearBox = new GearBox
                    {
                        Type = "Auto"
                    }
                }
            };

            Check.That(instance.GetPropertyValueByName(nameof(Car.Wheels)))
                .IsEqualTo(4);

            Check.That(instance.GetPropertyValueByName(
                    nameof(Car.Motor),
                    nameof(Motor.GearBox),
                    nameof(GearBox.Type)))
                .IsEqualTo("Auto");
        }

        [Fact]
        public void ShouldGetValuesFromList()
        {
            var instance = new Car
            {
                Doors = new List<Door> {
                    new Door(), 
                    new Door {IsOpen = true}
                }
            };

            Check.That(instance.GetPropertyValueByName("Doors", "1", "IsOpen"))
                .IsEqualTo(true);
        }
    }
}