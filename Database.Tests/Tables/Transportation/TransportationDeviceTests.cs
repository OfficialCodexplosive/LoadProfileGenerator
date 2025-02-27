﻿using System;
using System.Collections.ObjectModel;
using Automation;
using Common;
using Common.Tests;
using Database.Tables.BasicHouseholds;
using Database.Tables.Transportation;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;


namespace Database.Tests.Tables.Transportation {

    public class TransportationDeviceTests : UnitTestBaseClass {
        [Fact]
        [Trait(UnitTestCategories.Category,UnitTestCategories.BasicTest)]
        public void TransportationDeviceTest()
        {
            using (var db = new DatabaseSetup(Utili.GetCurrentMethodAndClass()))
            {
                db.ClearTable(TransportationDeviceCategory.TableName);
                db.ClearTable(TransportationDevice.TableName);
                var tdc = new TransportationDeviceCategory("tdc1", null, db.ConnectionString, "desc", true, Guid.NewGuid().ToStrGuid());
                tdc.SaveToDB();
                VLoadType chargingloadType = (VLoadType)VLoadType.CreateNewItem(null, db.ConnectionString);
                chargingloadType.SaveToDB();
                var sl = new TransportationDevice("name", null, db.ConnectionString, "desc", 1, SpeedUnit.Kmh, tdc, 100, 10, 10, 10, chargingloadType, Guid.NewGuid().ToStrGuid());
                var slocs = new ObservableCollection<TransportationDevice>();

                sl.SaveToDB();

                var categories = new ObservableCollection<TransportationDeviceCategory> {
                tdc
            };
                var loadTypes = new ObservableCollection<VLoadType>();
                var mylt = new VLoadType("myloadtype", "", "W", "kWh", 1, 1, new TimeSpan(1, 0, 0), 1, db.ConnectionString, LoadTypePriority.RecommendedForHouseholds, true, Guid.NewGuid().ToStrGuid());
                mylt.SaveToDB();
                loadTypes.Add(mylt);
                loadTypes.Add(chargingloadType);
                sl.AddLoad(mylt, 10);

                TransportationDevice.LoadFromDatabase(slocs, db.ConnectionString, false, categories, loadTypes);
                db.Cleanup();
                (slocs.Count).Should().Be(1);
                TransportationDevice td = slocs[0];
                (td.ChargingLoadType).Should().Be(chargingloadType);
            }
        }


        [Fact]
        [Trait(UnitTestCategories.Category,UnitTestCategories.BasicTest)]
        public void TransportationDeviceImportTest()
        {
            using (var db1 = new DatabaseSetup(Utili.GetCurrentMethodAndClass() + "1"))
            {
                using (var db2 = new DatabaseSetup(Utili.GetCurrentMethodAndClass() + "2"))
                {
                    db1.ClearTable(TransportationDeviceCategory.TableName);
                    db1.ClearTable(TransportationDevice.TableName);
                    var srcSim = new Simulator(db2.ConnectionString);
                    var dstSim = new Simulator(db1.ConnectionString);
                    foreach (var device in srcSim.TransportationDevices.Items)
                    {
                        TransportationDevice.ImportFromItem(device, dstSim);
                    }
                }
            }
        }

        public TransportationDeviceTests([JetBrains.Annotations.NotNull] ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }
    }
}