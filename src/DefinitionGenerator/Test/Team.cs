#if DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DefinitionGenerator.Test
{
    public class Team
    {
        public string Name { get; set; }
        public People Leader { get; set; }
        public List<People> Members { get; set; }
        public List<People> SubMembers { get; set; }
        public Data SampleData { get; set; }
    }

    public class Data
    {
        public int ID { get; set; }
        public float Value { get; set; }
    }

    public class People
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }

    public static class TestExtension
    {
        public static People FromAltString(this People source, string alt)
        {
            var param = alt.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var people = new People();
            people.Name = param[0].Trim();
            people.Age = int.Parse(param[1].Trim());
            return people;
        }
    }
}
#endif
