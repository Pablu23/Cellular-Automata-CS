using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CellularAutomata
{
    // Zum Anzeigen des Bildes in einer Synchronen weise
    static class BufferedDisplay
    {
        // Zeigt das Bild und verwaltet alle Schritte um dies zu ermöglichen
        public static void ShowDisplay(Cell[,] cells)
        {
            // GetString Methode wird aufgerufen, um alle Zellen in einen String zu bekommen
            string outputToCon = GetString(cells);
            // Der aktive Bereich zum schreiben wird auf 0,0 gesetzt um das vorherige Bild zu ersetzen
            Console.SetCursorPosition(0, 0);
            // Bild wird auf der Konsole ausgegeben
            Console.WriteLine(outputToCon);
            return;
        }

        // Die Zellen werden in einen String geschrieben um das Bild anzeigen zu können
        private static string GetString(Cell[,] cells)
        {
            // Strinbuilder, weil schneller als "+="
            StringBuilder sb = new StringBuilder();

            for (int y = 0; y < cells.GetUpperBound(1) + 1; y++)
            {
                for (int x = 0; x < cells.GetUpperBound(0) + 1; x++)
                {
                    // Hinzufügen eines "X" wenn Zelle am Leben / eines " " (Leerzeichens) wenn Zelle Tot
                    sb.Append(cells[x, y].Alive ? "X" : " ");
                }
                // Linienende, es muss auf der nächsten Linie weiter geschrieben werden sonst macht das keinen sinn
                sb.Append("\n");
            }
            // Rückgabe an Verwalter um String auf Konsole auszugeben
            return sb.ToString();
        }
    }

    // Das Herzstück des Programmes, ohne das nichts laufen würde
    class Cell
    {
        // Ob die Zelle am leben oder Tot ist
        public bool Alive { get => _alive; set => _alive = value; }
        private bool _alive;

        public bool Pattern = false;
        // Durch synchronität ob die Zelle am ende der "Runde" sterben wird oder nicht
        private bool _gonnaDie;
        // Durch synchronität ob die Zelle am ende der "Runde" wiederbelebt wird oder nicht
        private bool _gonnaRespawn;
        private readonly int _x;
        private readonly int _y;


        private bool[] _latest = new bool[12];

        // Die Nachbarn der Zelle, sehr wichtig für die Berechnungen ob eine Zelle stirbt / lebt / wiederbelebt wird
        public List<Cell> _neighbours = new List<Cell>();
        

        public Cell(int x, int y, bool alive)
        {
            _alive = alive;
            _gonnaDie = false;
            _x = x;
            _y = y;
            _gonnaDie = false;
            _gonnaRespawn = false;
        }

        // Findung der Nachbarn
        public void FindNeighbours(Cell[,] allCells)
        // TODO: Modular machen
        {
            if (_x - 1 >= 0)
                _neighbours.Add(allCells[_x - 1, _y]);
            if (_y - 1 >= 0)
                _neighbours.Add(allCells[_x, _y - 1]);
            if (_x + 1 <= allCells.GetUpperBound(0))
                _neighbours.Add(allCells[_x + 1, _y]);
            if (_y + 1 <= allCells.GetUpperBound(1))
                _neighbours.Add(allCells[_x, _y + 1]);
            if (_y - 1 >= 0 & _x - 1 >= 0)
                _neighbours.Add(allCells[_x - 1, _y - 1]);
            if (_y + 1 <= allCells.GetUpperBound(1) & _x + 1 <= allCells.GetUpperBound(0))
                _neighbours.Add(allCells[_x + 1, _y + 1]);
            if (_x + 1 <= allCells.GetUpperBound(0) && _y - 1 >= 0)
                _neighbours.Add(allCells[_x + 1, _y - 1]);
            if (_y + 1 <= allCells.GetUpperBound(1) && _x - 1 >= 0)
                _neighbours.Add(allCells[_x - 1, _y + 1]);
        }

        // Zelle wird makiert für Tot / leben / wiederbelebung
        public void DeadOrAlive()
        {
            int counter = 0;
            // Es wird gezählt wieviele Lebende Nachbarn eine Zelle hat
            foreach (var item in _neighbours)
            {
                if (item._alive)
                {
                    counter++;
                }
            }

            // Um rechenleistung zu schonen wird nur geguckt ob die Zelle stirbt, wenn sie auch lebt um sterben zu Können
            if (_alive)
            {
                // Anpassungsmöglichkeiten wann eine Zelle stirbt
                if (counter < 2 || counter > 3)
                {
                    _gonnaDie = true;
                }
                // Oder wann nicht
                else
                {
                    _gonnaDie = false;
                }
            }
            if (!_alive && counter == 3)
            {
                _gonnaRespawn = true;
            }
        }

        // Synchrones sterben / wiederbeleben / Nichts passieren
        public void Die()
        {
            // Pattern recognition, damit man nicht alle 3000 Runden warten muss wenn es nur noch statisch ist
            Pattern = true;

            _latest[0] = _latest[1];
            _latest[1] = _latest[2];
            _latest[2] = _latest[3];
            _latest[3] = _latest[4];
            _latest[4] = _latest[5];
            _latest[5] = _latest[6];

            _latest[6] = _latest[7];
            _latest[7] = _latest[8];
            _latest[8] = _latest[9];
            _latest[9] = _latest[10];
            _latest[10] = _latest[11];
            _latest[11] = _alive;

            for (int i = 0; i < 5; i++)
            {
                if (!_latest[i] == _latest[i + 6])
                {
                    Pattern = false;
                }
            }
            // Wird sterben
            if (_gonnaDie == true)
            {
                _alive = false;
            }
            // Wird wiederbelebt
            if (_gonnaRespawn == true)
            {
                _alive = true;
                _gonnaRespawn = false;
            }
            _gonnaDie = false;
        }
    }

    // Einlese System von
    class AutomataReader 
    {
        public string[] lines;
        private readonly int _maxX;
        private readonly int _maxY;

        public AutomataReader(string path, int x = 0, int y = 0)
        {
            // Die Datei wird eingelesen und in einen String gepackt
            lines = System.IO.File.ReadAllLines(path); 
            _maxX = x;
            _maxY = y;
        }

        // Eher veraltet / findet die längste linie um die variable maxX zu setzen
        public int FindLongestLine() 
        {
            int longestLine = 0;
            int tmpCounter = 0;
            foreach (var line in lines)
            {
                foreach (var chr in line)
                {
                    tmpCounter++;
                }
                longestLine = tmpCounter > longestLine ? tmpCounter : longestLine;
                tmpCounter = 0;
            }
            return longestLine;
        }

        // Umwandlung des Strings in Zellen (Obj)
        public Cell[,] FromFile() 
        {
            Cell[,] output;

            // Wenn angabe für größe des "Brettes" gegeben wird dieses Brett auf die Angabe gesetzt
            if (_maxX > 0 && _maxY > 0)
            {
                output = new Cell[_maxX, _maxY]; 
            }
            // Falls keine Angabe wird die Größe des "Brettes" auf die Längste Linie gesetzt und wieviele Linien es gibt
            else
            {
                output = new Cell[FindLongestLine(), lines.Length]; 
            }
            //Initialisierung der Zellen
            for (int i = 0; i < output.GetUpperBound(0) + 1; ++i)
            {
                for (int j = 0; j < output.GetUpperBound(1) + 1; j++)
                {
                    output[i, j] = new Cell(i, j, false);
                }
            }

            int x = 0, y = 0;
            // Setzung der Zellen auf Lebendig oder Tot abhängig davon was in der Datei angegeben wurde
            foreach (var line in lines)
            {
                foreach (var chr in line)
                {
                    output[x, y].Alive = chr == 'X'; 
                    x++;
                }
                x = 0;
                y++;
            }
            return output;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            // Alle Zellen
            Cell[,] allCells;

            // Stopwatch zum Ermöglichen von gleich langen "Runden"
            Stopwatch sw = new Stopwatch(); 
            Random r = new Random();

            // Angabe wie groß das Feld mit Zellen sein soll in Vertikal. Horizontal = Vertikal * 2
            int size = 60; 
            bool loadFromFile = false;
            long timeToNextFrame = 100;
            bool done = false;
            bool patternRecognition = true;

            Restart:
            Console.Clear();
            while (true)
            {
                Console.WriteLine("Welcome to my Cellular Automata simulation.");
                Console.WriteLine("-------------------------------------------");
                Console.WriteLine("[S]tart");
                Console.WriteLine("S[e]ttings");
                Console.WriteLine("E[n]d");
                string input = Console.ReadLine();
                Console.Clear();

                switch (input.ToLower())
                {
                    case "s":
                        goto Start; //DONT DO THIS!!!!

                    case "e":
                        while (!done)
                        {
                            Console.WriteLine("[S]ize of Field: " + size);
                            Console.WriteLine("[L]oad from File: " + loadFromFile);
                            Console.WriteLine("[T]ime to next Frame: " + timeToNextFrame);
                            Console.WriteLine("[Q]uit to Main Menu");
                            string input2 = Console.ReadLine();

                            switch (input2.ToLower())
                            {
                                case "s":
                                    Console.WriteLine("Enter new Size");
                                    string newSize = Console.ReadLine();
                                    if (!int.TryParse(newSize, out size))
                                    {
                                        Console.WriteLine("Input wasnt accepted. Please try again.");
                                        Console.ReadKey(true);
                                    }
                                    break;

                                case "l":
                                    Console.WriteLine("Enter [T]rue / [F]alse");
                                    loadFromFile = Console.ReadLine().ToLower() == "t" ? true : false;
                                    break;

                                case "t":
                                    Console.WriteLine("Enter new time to next Frame");
                                    string newttnf = Console.ReadLine();
                                    if (!long.TryParse(newttnf, out timeToNextFrame))
                                    {
                                        Console.WriteLine("Input wasnt accepted. Please try again.");
                                    }
                                    break;

                                case "q":
                                    done = true;
                                    break;

                                default:
                                    Console.WriteLine("Your input wasnt recognised please try again.");
                                    break;
                            }
                            Console.Clear();
                        }
                        done = false;
                        break;

                    case "n":
                        System.Environment.Exit(0);
                        break;

                    default:
                        Console.WriteLine("Wrong input");
                        break;
                }
                Console.Clear();
            }
            // TODO: Menu und Settings verbessern
            Start:
            // Initialisierungsprozess
            // Check ob Input Datei vorhanden falls man ein Eigenes Design Importieren möchte
            if (loadFromFile && File.Exists(@"XXXXXX\Manual.txt"))                                                                                                                 
            {
                // TODO: -> zu Static klasse
                AutomataReader automataReader = new AutomataReader(@"XXXXXX\Manual.txt", 60, 180);
                // Der richtige Import von der Datei
                allCells = automataReader.FromFile(); 
            }
            else
            {
                // Größe des Feldes der Zellen wird gesetzt
                allCells = new Cell[size * 2, size]; 
                for (int x = 0; x < allCells.GetUpperBound(0) + 1; x++)
                {
                    for (int y = 0; y < allCells.GetUpperBound(1) + 1; y++)
                    {
                        allCells[x, y] = new Cell(x, y, r.Next(0, 100) > 30 ? false : true);
                    }
                }
            }

            // Warten auf eingabe um Positionierung der Konsole zu ermöglichen
            Console.ReadKey(true); 
            for (int x = 0; x < allCells.GetUpperBound(0) + 1; x++)
            {
                for (int y = 0; y < allCells.GetUpperBound(1) + 1; y++)
                {
                    // Jeder Zelle werden seine Nachbarn zugewiesen
                    allCells[x, y].FindNeighbours(allCells); 
                }
            }

            // Zeigen des Aufbaus der Zellen mit einem Standbild um möglicherweise zu reproduzieren des Versuches
            BufferedDisplay.ShowDisplay(allCells);
            // Warten auf eingabe um das Programm starten zu lassen
            Console.ReadKey(true); 

            patternRecognition = true;

            // So oft wie die Anzahl es bestimmt, meistens befindet sich das Bild aber in einem "Stillstand", dadurch das alle Zellen Tod sind
            // Oder nicht sterben können
            for (int i = 0; i < 3000; i++) 
            {
                if (i > 12 && patternRecognition)
                {
                    bool foundPattern = true;
                    foreach (var item in allCells)
                    {
                        if (!item.Pattern)
                        {
                            foundPattern = false;
                            break;
                        }
                    }
                    if (foundPattern)
                    {
                        Console.WriteLine("Pattern found!");
                        Console.ReadKey(true);
                        goto Restart;
                    }
                }

                int height = allCells.GetLength(0);
                int width = allCells.GetLength(1);

                Parallel.For(0, height, y =>
                {
                    for (int x = 0; x < width; x++)
                    {
                        // Zelle wird zum sterben / überleben / wiedergeboren werden makiert
                        // Dieser Step ist notwendig, damit die Zellen nicht asynchron von einander operieren
                        allCells[y, x].DeadOrAlive();                            
                    }
                });

                // Zeigt den Bildschirm
                BufferedDisplay.ShowDisplay(allCells); 

                Parallel.For(0, height, y =>
                {
                    for (int x = 0; x < width; x++)
                    {
                        // Lässt die Zelle sterben falls sie stirbt / Zelle wird geboren falls sie geboren werden soll
                        allCells[y, x].Die(); 
                    }
                });

                // Um für eine "Runde" immer gleich viel Zeit zu brauchen, wird gemessen wie lange alles gebraucht hat und die restlichen ms werden dann abgewartet
                sw.Stop();

                // Prüfung ob die "Runde" länger als vorgeschriebe Zeit gedauert hat
                if (sw.ElapsedMilliseconds < timeToNextFrame)
                {
                    // Wenn nicht dann wird die Zeit hier von der vorgeschriebenen Zeit subtrahiert
                    long time = timeToNextFrame - sw.ElapsedMilliseconds;
                    // Und die rest Zeit wird hier gewartet, um gleich Lange "Runden" zu garantieren
                    System.Threading.Thread.Sleep((int)time); 
                }
                // Neustart der Zeit messung
                sw.Restart(); 
            }
            goto Restart; //DONT DO THIS!!!!
        }
    }
}