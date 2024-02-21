namespace AxorP1.Class
{
    public class PanelObject
    {
        public string Id { get; set; } // Id of the panel
        public string Title { get; set; } // Header text of the panel
        public int Column { get; set; } // Position on the column
        public int Row { get; set; }  // Position on the row
        public int SizeX { get; set; } = 1; // Horizontal size (number of cells)
        public int SizeY { get; set; } = 1; // Vertical size (number of cells)
        public int MinSizeX { get; set; } = 0; // Minimum horizontal size 
        public int MinSizeY { get; set; } = 0; // Minimum vertical size 

        public System.Type ComponentType { get; set; } // Type of the child component

        public Dictionary<string, object> Parameters { get; set; } // Parameters for the child component
    }
}
