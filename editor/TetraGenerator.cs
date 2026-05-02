using System.Collections.Generic;

namespace editor
{
    public class Tetra
    {
        public int Index { get; set; }
        public string Op { get; set; }
        public string Arg1 { get; set; }
        public string Arg2 { get; set; }
        public string Result { get; set; }

        public override string ToString()
        {
            return $"{Index}: ({Op}, {Arg1}, {Arg2}, {Result})";
        }
    }

    public class TetraGenerator
    {
        private List<Tetra> tetras;
        private int tempCounter;

        public List<Tetra> Tetras
        {
            get { return tetras; }
        }

        public TetraGenerator()
        {
            tetras = new List<Tetra>();
            tempCounter = 1;
        }

        public string NewTemp()
        {
            return $"t{tempCounter++}";
        }

        public string AddTetra(string op, string arg1, string arg2)
        {
            string result = NewTemp();
            tetras.Add(new Tetra
            {
                Index = tetras.Count + 1,
                Op = op,
                Arg1 = arg1,
                Arg2 = arg2,
                Result = result
            });
            return result;
        }

        public void Clear()
        {
            tetras.Clear();
            tempCounter = 1;
        }
    }
}