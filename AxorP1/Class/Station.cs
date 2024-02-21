namespace AxorP1.Class
{
    public class Station
    {
        public string Id { get; set; } 
        public DateTime DateTime { get; set; } // Date
        public string StationName { get; set; } // Nom de la station (string)
        public double UpstreamLevel { get; set; } // Niveau amont (mètres)
        public double DownstreamLevel { get; set; } // Niveau aval (mètres)
        public double CentralProduction { get; set; } // Production centale (mW)
        public double FallHeight { get; set; } // Hauteur de chute (mètres)
        public double TotalFlowRate { get; set; } // Débit Total (M2/sec)
        public double MonthlyProductionTarget { get; set; } // Production objectif mensuel
        public double AnnualProductionTarget { get; set; } // Production objectif annuel
        public double MonthlyProductionActual { get; set; } // Production réalisation mensuelle
        public double AnnualProductionActual { get; set; } // Production réalisation annuelle
        public double MonthlyPercentage
        { // Pourcentage mensuel (%)
            get
            {   // Champ calculé
                return (MonthlyProductionTarget != 0) ? (MonthlyProductionActual / MonthlyProductionTarget) : 0;
            }
        }
        public double AnnualPercentage
        {            // Pourcentage annuel (%)
            get
            {   // Champ calculé
                return (AnnualProductionTarget != 0) ? (AnnualProductionActual / AnnualProductionTarget) : 0;
            }
        }

        public List<Group> Groups { get; set; } // List of Group objects
    }
}
