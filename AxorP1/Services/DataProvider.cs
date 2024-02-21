using System.Collections.ObjectModel;
using AxorP1.Class;
using Syncfusion.Blazor.Data;
using static AxorP1.Shared.Components.Panels.PieChartComponent;

namespace AxorP1.Services
{
    public class DataProvider
    {
        public Dictionary<string, string> stationNames = new Dictionary<string, string>
        {
            { "CAG", "Chutes-à-Gorry" },
            { "JG", "Jean-Guérin" },
            { "HC", "Hydro-Canyon" },
            { "PB", "Petites-Bergeronnes" },
            { "SERF", "Franquelin" },
            { "SERS", "Sheldrake" },
            { "LR", "Long Rapids" },
            { "TF", "Twin Falls"  }
        };

        // Axor hydroelectric power stations within Canada
        private List<StationMapData> MapDetails = new List<StationMapData>
        {
            new StationMapData { Latitude = 46.78983433499708, Longitude = -72.00353219004116 }, // Chutes-à-Gorry
            new StationMapData { Latitude = 46.63504334946685, Longitude = -71.044330699399 }, // Jean-Guérin 
            new StationMapData { Latitude = 47.055080489178124, Longitude = -70.84331694178222 }, // Hydro-Canyon  
            new StationMapData { Latitude = 48.262531954209, Longitude = -69.63143815475888 }, // Petites-Bergeronnes 
            new StationMapData { Latitude = 49.298872387059106, Longitude = -67.84395606024589 }, // Franquelin 
            new StationMapData { Latitude = 50.28440380362198, Longitude = -64.9309060547453 }, // Sheldrake 
            new StationMapData { Latitude = 48.60275472499946, Longitude = -88.78180622752375 }, // Long Rapids
            new StationMapData { Latitude = 48.60275472499946, Longitude = -88.88180622752375 }, // Twin Falls
        };

        public DataProvider() 
        {
            for(var i = 0; i < stationNames.Count; i++)
            {
                MapDetails[i].Name = stationNames.ElementAt(i).Value;
            }
        }

        // Method to get the data
        public async Task<ObservableCollection<Station>> GetDataAsync()
        {
            ObservableCollection<Station> Data = new ObservableCollection<Station>();
            Random random = new Random();

            return await Task.Run(() =>
            {
                for (int i = 0; i < stationNames.Count; i++)
                {
                    double num = random.Next(1, 8);

                    var station = new Station
                    {
                        Id = stationNames.ElementAt(i).Key,
                        DateTime = DateTime.Now,
                        StationName = stationNames.ElementAt(i).Value,
                        UpstreamLevel = 60 - num,
                        DownstreamLevel = 20 - num,
                        CentralProduction = 100 - 10 * num,
                        FallHeight = 15 - num,
                        TotalFlowRate = 20 - num,
                        MonthlyProductionTarget = 150 - 10 * num,
                        AnnualProductionTarget = 1800 - 100 * num,
                        MonthlyProductionActual = 120 - 10 * num,
                        AnnualProductionActual = 1500 - 100 * num,

                        Groups = new List<Group>()
                    };

                    // Add the first group
                    station.Groups.Add(new Group
                    {
                        StationName = stationNames.ElementAt(i).Key,
                        GroupName = "Group 1",
                        FlowRate = 18 - num,
                        GroupTA = true,
                        Production = 70 + num,
                        FineGridDifferential = 10 - num,
                        CoarseGridDifferential = 13 - num
                    });

                    // Add the second group only if the condition is met
                    if (i % 2 == 0)
                    {
                        station.Groups.Add(new Group
                        {
                            StationName = stationNames.ElementAt(i).Key,
                            GroupName = "Group 2",
                            FlowRate = 20 - i,
                            GroupTA = false,
                            Production = 50 + num,
                            FineGridDifferential = 11 - num,
                            CoarseGridDifferential = 16 - num
                        });
                    }

                    Data.Add(station);
                }

                return Data;
            });


        }

        // Method to get all groups from the existing Data property
        public ObservableCollection<Group> GetAllGroups(IEnumerable<Station> Data, int? filterGroupNum = null)
        {
            ObservableCollection<Group> allGroups = new ObservableCollection<Group>();

            foreach (var station in Data)
            {
                foreach (var group in station.Groups)
                {
                    allGroups.Add(group);
                }
            }

            if (filterGroupNum != null && filterGroupNum > 0 && filterGroupNum <= 2 )
            {
                var filterGroupName = $"Group {filterGroupNum}";
                // Returns only group 1 or group 2
                return new ObservableCollection<Group>(allGroups.Where(d => d.GroupName == filterGroupName));
            }

            return allGroups;
        }


        // Method to get the data of the past for one station 
        public async Task<ObservableCollection<Station>> GetPastDataAsync(string id)
        {
            if (stationNames.TryGetValue(id, out string stationName) == false) 
            { return new ObservableCollection<Station>(); } // Wrong id

            ObservableCollection<Station> PastData = new ObservableCollection<Station>();
            Random random = new Random();

            return await Task.Run(() =>
            {
                for (int year = 0; year <= 3; year++) // Years
                {
                    for (int i = 0; i < 12; i++) // Months
                    {
                        double num = random.Next(1, 8);

                        var station = new Station
                        {
                            Id = id,
                            DateTime = DateTime.Now.AddMonths(-i).AddYears(-year),
                            StationName = stationName,
                            UpstreamLevel = 60 - num,
                            DownstreamLevel = 20 - num,
                            CentralProduction = 100 - 10 * num,
                            FallHeight = 15 - num,
                            TotalFlowRate = 20 - num,
                            MonthlyProductionTarget = 150 - 10 * num,
                            AnnualProductionTarget = 1800 - 100 * num,
                            MonthlyProductionActual = 120 - 10 * num,
                            AnnualProductionActual = 1500 - 100 * num,

                            Groups = new List<Group>()
                        };

                        // Ajouter le premier groupe
                        station.Groups.Add(new Group
                        {
                            StationName = id,
                            GroupName = "Group 1",
                            FlowRate = 18 - num,
                            GroupTA = true,
                            Production = 70 + num,
                            FineGridDifferential = 10 - num,
                            CoarseGridDifferential = 13 - num
                        });

                        // Ajouter le deuxième groupe uniquement si la condition est remplie
                        if (stationNames.IndexOf(new KeyValuePair<string, string>(id, stationName)) % 2 == 0)
                        {
                            station.Groups.Add(new Group
                            {
                                StationName = id,
                                GroupName = "Group 2",
                                FlowRate = 20 - i,
                                GroupTA = false,
                                Production = 50 + num,
                                FineGridDifferential = 11 - num,
                                CoarseGridDifferential = 16 - num
                            });
                        }

                        PastData.Add(station);
                    }
                }

                return PastData;
            });
        }

        public List<StationMapData> GetMapDetails()
        {
            return MapDetails;
        }

        // Calculate the percentage of production generated by each station
        public ObservableCollection<PieData> GetProductionStatistics(IEnumerable<Station> Data)
        {
            // Calculate the total production across all stations
            double totalProduction = Data.Sum(station => station.CentralProduction);

            var pieDataList = new ObservableCollection<PieData>();

            // Calculate the percentage for each station
            foreach (var station in Data)
            {
                double percentage = (totalProduction != 0) ? (station.CentralProduction / totalProduction) * 100 : 0;
                percentage = Math.Round(percentage, 2);
                pieDataList.Add(new PieData { Name = station.Id, Percentage = percentage });
            }

            return pieDataList;
        }


    }
}
