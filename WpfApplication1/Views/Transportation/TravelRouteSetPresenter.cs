﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows;
using Automation.ResultFiles;
using Common;
using Common.Enums;
using Database.Tables.BasicElements;
using Database.Tables.BasicHouseholds;
using Database.Tables.ModularHouseholds;
using Database.Tables.Transportation;
using JetBrains.Annotations;
using LoadProfileGenerator.Presenters;
using LoadProfileGenerator.Presenters.BasicElements;

namespace LoadProfileGenerator.Views.Transportation {
    public class TravelRouteSetPresenter : PresenterBaseDBBase<TravelRouteSetView> {
        [CanBeNull] private DataTable _connectionCountTable;
        [CanBeNull] private DataTable _distanceTable;
        [NotNull] private ModularHousehold _modularHousehold;
        private TravelRoute _selectedTravelRoute;

        public TravelRouteSetPresenter([NotNull] ApplicationPresenter applicationPresenter, [NotNull] TravelRouteSetView view,
                                       [NotNull] TravelRouteSet routeSet) : base(view, "ThisRouteSet.Name", routeSet,
            applicationPresenter)
        {
            ThisRouteSet = routeSet;
            _modularHousehold = Sim.ModularHouseholds[0];
            _selectedInputOption = InputOptions.Keys.First();
            RefreshVisibility();
            RefreshDataTable();
            RefreshRoutes();
        }

        [ItemNotNull]
        [NotNull]
        public Dictionary<string, TravelRouteEntryInputOption> InputOptions { get; } = InitInputOptions();

        /// <summary>
        /// Is used to define a readable string version for each enum item for the different input options
        /// </summary>
        /// <returns>A dictionary mapping the readable string versions to the respective enum values</returns>
        private static Dictionary<string, TravelRouteEntryInputOption> InitInputOptions()
        {
            var d = new Dictionary<string, TravelRouteEntryInputOption> {
                { "Filter by age and gender", TravelRouteEntryInputOption.AgeAndGender },
                { "Only for one person", TravelRouteEntryInputOption.Person }
            };
            // check if all enum items are covered
            if (d.Count != Enum.GetNames(typeof(TravelRouteEntryInputOption)).Length)
            {
                throw new LPGNotImplementedException("Did not specify a readable string version for all items of enum " + nameof(TravelRouteEntryInputOption));
            }
            return d;
        }

        [ItemNotNull]
        [NotNull]
        [UsedImplicitly]
        public ObservableCollection<ModularHousehold> AllHouseholds =>
            Sim.ModularHouseholds.Items;

        [UsedImplicitly]
        [JetBrains.Annotations.NotNull]
        public IEnumerable<AffordanceTaggingSet> AffordanceTaggingSets
        {
            get
            {
                return Sim.AffordanceTaggingSets.Items;
            }
        }

        private string _selectedInputOption;
        public string SelectedInputOption { 
            get
            {
                return _selectedInputOption;
            }
            set
            {
                _selectedInputOption = value;
                RefreshVisibility();
            }
        }

        /// <summary>
        /// Resets the visibility of some input controls, depending on the currently selected input option
        /// </summary>
        private void RefreshVisibility()
        {
            var inputType = InputOptions[SelectedInputOption];
            ShowAgeAndGenderInputs = inputType == TravelRouteEntryInputOption.AgeAndGender ? Visibility.Visible : Visibility.Collapsed;
            ShowPersonDropDown = inputType == TravelRouteEntryInputOption.Person ? Visibility.Visible : Visibility.Collapsed;
        }

        [ItemNotNull]
        [NotNull]
        [UsedImplicitly]
        public ObservableCollection<TravelRoute> AvailableTravelRoutes { get { return Sim.TravelRoutes.Items; } }

        public int MinimumAge { get; set; }

        public int MaximumAge { get; set; } = 100;

        public PermittedGender SelectedGender { get; set; } = PermittedGender.All;

        [UsedImplicitly]
        [JetBrains.Annotations.NotNull]
        public IEnumerable<PermittedGender> PermittedGenders
        {
            get
            {
                return Enum.GetValues<PermittedGender>().Cast<PermittedGender>();
            }
        }

        public AffordanceTag SelectedAffordanceTag { get; set; }

        private bool _affordanceTagCmbBoxEnabled;
        public bool AffordanceTagCmbBoxEnabled
        {
            get
            {
                return _affordanceTagCmbBoxEnabled;
            }
            set
            {
                if (value != _affordanceTagCmbBoxEnabled)
                {
                    _affordanceTagCmbBoxEnabled = value;
                    OnPropertyChanged(nameof(AffordanceTagCmbBoxEnabled));
                }
            }
        }

        /// <summary>
        /// Refreshes the IsEnabled state of the affordance tag combobox.
        /// The affordance tag combobox is only enabled if an affordance tagging set is selected.
        /// </summary>
        public void RefreshAffordanceTagCmbBoxIsEnabled()
        {
            AffordanceTagCmbBoxEnabled = ThisRouteSet.AffordanceTaggingSet != null;
        }

        public Person SelectedPerson { get; set; }

        [NotNull]
        [UsedImplicitly]
        [ItemNotNull]
        public ObservableCollection<Person> AvailablePersons { get { return Sim.Persons.Items; } }

        public double Weight { get; set; } = 1.0;

        [NotNull]
        [UsedImplicitly]
        public DataTable ConnectionCountTable {
            get => _connectionCountTable ?? throw new LPGException("ConnectionCountTable was null");
            set {
                if (Equals(value, _connectionCountTable)) {
                    return;
                }

                _connectionCountTable = value;
                OnPropertyChanged(nameof(ConnectionCountTable));
            }
        }

        [NotNull]
        [UsedImplicitly]
        public DataTable DistanceTable {
            get => _distanceTable ?? throw new LPGException("DistanceTable was null");
            set {
                if (Equals(value, _distanceTable)) {
                    return;
                }

                _distanceTable = value;
                OnPropertyChanged(nameof(DistanceTable));
            }
        }

        [NotNull]
        [UsedImplicitly]
        public ModularHousehold ModularHousehold {
            get => _modularHousehold;
            set {
                if (Equals(value, _modularHousehold)) {
                    return;
                }

                _modularHousehold = value;
                OnPropertyChanged(nameof(ModularHousehold));
            }
        }

        [ItemNotNull]
        [NotNull]
        [UsedImplicitly]
        public ObservableCollection<Site> Sites => Sim.Sites.Items;

        [NotNull]
        [UsedImplicitly]
        public TravelRouteSet ThisRouteSet { get; }

        public TravelRoute SelectedTravelRoute
        {
            get => _selectedTravelRoute;
            set {
                if (Equals(value, _selectedTravelRoute)) {
                    return;
                }

                _selectedTravelRoute = value;
                OnPropertyChanged(nameof(SelectedTravelRoute));
            }
        }

        [ItemNotNull]
        [NotNull]
        [UsedImplicitly]
        public ObservableCollection<UsedIn> UsedIns { get; } = new ObservableCollection<UsedIn>();

        private Visibility _showAgeAndGenderInputs;

        [UsedImplicitly]
        public Visibility ShowAgeAndGenderInputs
        {
            get
            {
                return _showAgeAndGenderInputs;
            }
            set
            {
                _showAgeAndGenderInputs = value;
                OnPropertyChanged(nameof(ShowAgeAndGenderInputs));
            }
        }

        private Visibility _showPersonDropDown;

        [UsedImplicitly]
        public Visibility ShowPersonDropDown
        {
            get
            {
                return _showPersonDropDown;
            }
            set
            {
                _showPersonDropDown = value;
                OnPropertyChanged(nameof(ShowPersonDropDown));
            }
        }

        /// <summary>
        /// Sets the affordance tag of each TravelRouteSetEntry to null.
        /// This happens when the affordance set which is used in the current TravelRouteSet is changed.
        /// </summary>
        public void ResetSetEntryAffordanceTags()
        {
            if (ThisRouteSet.TravelRoutes == null)
            {
                return;
            }
            foreach (var entry in ThisRouteSet.TravelRoutes)
            {
                entry.AffordanceTag = null;
            }
        }

        public void Delete()
        {
            Sim.TravelRouteSets.DeleteItem(ThisRouteSet);
            Close(false);
        }

        public override bool Equals(object obj)
        {
            var presenter = obj as TravelRouteSetPresenter;
            return presenter?.ThisRouteSet.Equals(ThisRouteSet) == true;
        }

        public void FindMissingRoutesAndCreateThem()
        {
            var arr = RefreshMatrix(ModularHousehold, ThisRouteSet, out _);
            if (arr == null) {
                return;
            }

            var count = 0;
            for (var row = 1; row < arr.GetLength(0); row++) {
                for (var col = 1; col < arr.GetLength(1); col++) {
                    if (arr[row, col] == "0" && arr[row, 0] != arr[0, col]) {
                        Logger.Warning("Route between " + arr[row, 0] + " and " + arr[0, col] + " is missing.");
                        count++;
                        var route = Sim.TravelRoutes.CreateNewItem(Sim.ConnectionString);
                        var a = Sim.Sites.FindFirstByName(arr[row, 0]);
                        var b = Sim.Sites.FindFirstByName(arr[0, col]);
                        route.SiteA = a;
                        route.SiteB = b;
                        route.Name = "Route from " + a?.PrettyName + " to " + b?.PrettyName + " via ";
                        route.SaveToDB();
                        ApplicationPresenter.OpenItem(route);
                    }
                }
            }

            Logger.Error(count + " routes are missing.");
        }

        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                const int hash = 17;
                // Suitable nullity checks etc, of course :)
                return hash * 23 + TabHeaderPath.GetHashCode();
            }
        }

        public void RefreshDataTable()
        {
            Logger.Info("Refreshing the connection table");

            string[,] arr = RefreshMatrix(ModularHousehold, ThisRouteSet, out var distanceMatrix);
            if (arr == null) {
                // set contains no routes: show empty tables
                ConnectionCountTable = new DataTable();
                DistanceTable = new DataTable();
                return;
            }

            if (_connectionCountTable != null && _connectionCountTable.Rows.Count == arr.GetLength(1) &&
                ConnectionCountTable.Columns.Count == arr.GetLength(0)) {
                for (var row = 0; row < arr.GetLength(1); row++) {
                    var dr = ConnectionCountTable.Rows[row];
                    for (var col = 0; col < arr.GetLength(0); col++) {
                        var val = arr[col, row];
                        if(val!= null ) {
                            dr[col] = val;
                        }
                    }
                }
            }
            else {
                var dt = new DataTable();
                for (var i = 0; i < arr.GetLength(0); i++) {
                    if (string.IsNullOrWhiteSpace(arr[i, 0])) {
                        dt.Columns.Add("Location", typeof(string));
                    }
                    else {
                        dt.Columns.Add(arr[i, 0], typeof(string));
                    }
                }

                for (var row = 1; row < arr.GetLength(1); row++) {
                    var dr = dt.NewRow();
                    for (var col = 0; col < arr.GetLength(0); col++) {
                        var val = arr[col, row];
                        if (val!= null) {
                            dr[col] =val ;
                        }
                    }

                    dt.Rows.Add(dr);
                }

                ConnectionCountTable = dt;
            }

            if (_distanceTable != null && _distanceTable.Rows.Count == distanceMatrix.GetLength(1)
                                      && DistanceTable.Columns.Count == distanceMatrix.GetLength(0)) {
                for (var row = 0; row < distanceMatrix.GetLength(1); row++) {
                    var dr = DistanceTable.Rows[row];
                    for (var col = 0; col < distanceMatrix.GetLength(0); col++) {
                        dr[col] = distanceMatrix[col, row];
                    }
                }
            }
            else {
                var dt = new DataTable();
                for (var i = 0; i < distanceMatrix.GetLength(0); i++) {
                    if (string.IsNullOrWhiteSpace(distanceMatrix[i, 0])) {
                        dt.Columns.Add("Location", typeof(string));
                    }
                    else {
                        dt.Columns.Add(distanceMatrix[i, 0], typeof(string));
                    }
                }

                for (var row = 1; row < distanceMatrix.GetLength(1); row++) {
                    var dr = dt.NewRow();
                    for (var col = 0; col < distanceMatrix.GetLength(0); col++) {
                        dr[col] = distanceMatrix[col, row];
                    }

                    dt.Rows.Add(dr);
                }

                DistanceTable = dt;
            }
        }

        [ItemCanBeNull]
        [CanBeNull]
        public static string[,] RefreshMatrix([CanBeNull] ModularHousehold mhh, [NotNull] TravelRouteSet set,
                                              [ItemNotNull] [NotNull] out string[,] distanceMatrix)
        {
            distanceMatrix = new string[1, 1];
            if (mhh == null) {
                return null;
            }

            var locations = mhh.CollectLocations();
            //this goes for the travel route sites since a location might be in multiple sites
            var usedSitesA1 = set.TravelRoutes.Select(x => x.TravelRoute.SiteA).ToList();
            var usedSitesB1 = set.TravelRoutes.Select(x => x.TravelRoute.SiteB).ToList();
            var allSites = new List<Site>();
            allSites.AddRange(usedSitesA1);
            allSites.AddRange(usedSitesB1);

            var distinctSites = allSites.Distinct().Where(x => x != null).ToList();
            if (distinctSites.Count == 0) {
                return null;
            }

            var locationsInSites = distinctSites.SelectMany(x => x.Locations.Select(y => y.Location)).ToList();
            if (locationsInSites.Distinct().Count() != locationsInSites.Count) {
                Logger.Error("Some locations are assigned to more than one site");
                foreach (Location location in locationsInSites) {
                    int count = 0;
                    foreach (Location inSite in locationsInSites) {
                        if (location == inSite) {
                            count++;
                        }
                    }

                    if (count > 1) {
                        var usedSites = distinctSites.Where(x => x.Locations.Any(y => y.Location == location)).ToList();
                        string s = "";
                        foreach (Site site in usedSites) {
                            s += site.PrettyName + ",";
                        }
                        Logger.Error("Location " + location.PrettyName + " is in " + count +  " sites:" + s);
                    }
                }
            }
            foreach (var hhloc in locations) {
                if (!locationsInSites.Contains(hhloc)) {
                    Logger.Error("Missing location for this route set from the household " + mhh.PrettyName + ": " + hhloc.PrettyName);
                }
            }

            var distinctsites = distinctSites.Distinct().OrderBy(c => c.Name).ToList();

            var routeArray = new string[distinctsites.Count + 1, distinctsites.Count + 1];
            var distanceArray = new string[distinctsites.Count + 1, distinctsites.Count + 1];
            var distanceArrayDou = new double[distinctsites.Count + 1, distinctsites.Count + 1];
            routeArray[0, 0] = string.Empty;
            for (var i = 0; i < distinctsites.Count; i++) {
                routeArray[0, i + 1] = distinctsites[i].PrettyName;
                routeArray[i + 1, 0] = distinctsites[i].PrettyName;
                distanceArray[0, i + 1] = distinctsites[i].PrettyName;
                distanceArray[i + 1, 0] = distinctsites[i].PrettyName;
            }

            for (var i = 0; i < distinctsites.Count; i++) {
                for (var j = 0; j < distinctsites.Count; j++) {
                    var a = distinctsites[i];
                    var b = distinctsites[j];
                    var routes = set.TravelRoutes
                        .Where(x => x.TravelRoute.SiteA == a && x.TravelRoute.SiteB == b).ToList();
                    routeArray[i + 1, j + 1] = routes.Count.ToString();
                    if (routes.Count > 0) {
                        double distance = routes.Select(x => x.CalculateTotalDistance()).Average();
                        distanceArray[i + 1, j + 1] = distance.ToString("F0");
                        distanceArrayDou[i + 1, j + 1] = distance;
                    }
                    else {
                        distanceArray[i + 1, j + 1] = "-";
                    }
                }
            }

            distanceMatrix = distanceArray;
            for (int row = 0; row < distanceArrayDou.GetLength(0); row++) {
                for (int col = 0; col < distanceArrayDou.GetLength(1); col++) {
                    if (Math.Abs(distanceArrayDou[row, col] - distanceArrayDou[col, row]) > 0.0000001) {
                        Logger.Warning("Distance from " + routeArray[0, col] + " to " +
                                       routeArray[row, 0] + " is no identical to the other way around: " +
                                       distanceArrayDou[row, col] + " vs. " + distanceArrayDou[col, row]
                        );
                    }
                }
            }

            return routeArray;
        }

        public void RefreshRoutes()
        {
            // A TravelRouteSet can now have multiple entries with the same route but different filter settings
        }

        public static void RefreshUsedIn()
        {
            /*
            Simulator s = Sim;
            List<UsedIn> usedIn = T.GetUsedIns(s.Households.MyItems, s.HouseholdTraits.It,
                s.Affordances.It, s.HouseTypes.It, s.DeviceActions.It);
            _usedIns.Clear();
            foreach (UsedIn dui in usedIn)
                _usedIns.Add(dui);*/
        }

        public void FindMissingRoutesForAllHouseholds()
        {
            foreach (var household in Sim.ModularHouseholds.Items) {
                var arr = RefreshMatrix(household, ThisRouteSet, out _);
                 if (arr == null)
                {
                    Logger.Error("Found errors in household " + household.PrettyName);
                    return;
                }
            }
        }

        public void MakeCopy()
        {
            var newSet = TravelRouteSet.ImportFromItem(ThisRouteSet, ApplicationPresenter.Simulator ?? throw new LPGException("Simulator not set"));
            var trs = ApplicationPresenter.Simulator.TravelRouteSets;
            string basename = ThisRouteSet.Name + "(Copy)";
            string name = basename;
            int i = 1;
            while ( trs.IsNameTaken(name))
            {
                name = name + " " + i++;
            }
            newSet.Name = name;
            newSet.SaveToDB();
            ApplicationPresenter.OpenItem(newSet);


        }

        public void RemoveWorkplaceRoutes()
        {
            var todelete = ThisRouteSet.TravelRoutes.Where(x => x.TravelRoute.Name.ToLower().Contains("workplace")).ToList();
            foreach (var entry in todelete) {
                ThisRouteSet.DeleteEntry(entry);
            }
            Logger.Info("Completed deleting " + todelete.Count + " entries.");
        }

        // helper function for the button "Add distance appropriate workplace routes..."
        // Adds all matching routes without filter restrictions
        public void AddDistanceMatchingWorkplaceRoutes()
        {
            List<TravelRoute> usedRoutes = ThisRouteSet.TravelRoutes.Select(x => x.TravelRoute ).ToList();
            var newRoutes = Sim.TravelRoutes.Items.Where(x => !usedRoutes.Contains(x)&& x.Name.ToLower().Contains("workplace")).ToList();
            var arr = ThisRouteSet.Name.Split(' ');
            var kmstr = arr.FirstOrDefault(x => x.EndsWith("km"));
            if (kmstr == null)
            {
                Logger.Error("No distance declaration found in the name");
                return;
            }
            newRoutes = newRoutes.Where(x => x.Name.Contains(kmstr)).ToList();
            foreach (var route in newRoutes) {
                // Use default parameter values to include all persons and apply the default weight of 1.0
                ThisRouteSet.AddRoute(route, -1, -1, PermittedGender.All, null, null, 1.0); 
            }
        }
    }

    public enum TravelRouteEntryInputOption
    {
        AgeAndGender,
        Person
    }
}
