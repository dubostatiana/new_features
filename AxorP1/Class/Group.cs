namespace AxorP1.Class
{
    public class Group
    {
        public string StationName { get; set; }  // Nom de la station (string)
        public string GroupName { get; set; } // Nom du groupe (string)
        public double FlowRate { get; set; } // Débit (M2/sec)
        public bool GroupTA { get; set; } // Groupe T/A (Actif ou Inactif)
        public double Production { get; set; } // Production (mW)
        public double FineGridDifferential { get; set; } // Différentiel de grille : fines (mètres)
        public double CoarseGridDifferential { get; set; } // Différentiel de grille : grossières (mètres)
    }
}
