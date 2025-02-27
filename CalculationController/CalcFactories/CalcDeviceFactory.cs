﻿//-----------------------------------------------------------------------

// <copyright>
//
// Copyright (c) TU Chemnitz, Prof. Technische Thermodynamik
// Written by Noah Pflugradt.
// Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
//
// Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
//  Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer
// in the documentation and/or other materials provided with the distribution.
//  All advertising materials mentioning features or use of this software must display the following acknowledgement:
//  “This product includes software developed by the TU Chemnitz, Prof. Technische Thermodynamik and its contributors.”
//  Neither the name of the University nor the names of its contributors may be used to endorse or promote products
//  derived from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE UNIVERSITY 'AS IS' AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING,
// BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE UNIVERSITY OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, S
// PECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; L
// OSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
// STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
// ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

// </copyright>

//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Automation.ResultFiles;
using CalculationController.DtoFactories;
using CalculationEngine.HouseholdElements;
using Common;
using Common.CalcDto;
using Common.JSON;
using Database.Tables.BasicElements;
using JetBrains.Annotations;

namespace CalculationController.CalcFactories {
    public class CalcDeviceFactory {
        [NotNull]
        private readonly AvailabilityDtoRepository _availabilityDtoRepository;

        [NotNull] private readonly CalcVariableRepository _calcVariableRepository;
        private readonly CalcRepo _calcRepo;

        [NotNull]
        private readonly CalcLoadTypeDictionary _loadTypeDictionary;

        public CalcDeviceFactory([NotNull] CalcLoadTypeDictionary loadTypeDictionary,
                                 [NotNull] AvailabilityDtoRepository availabilityDtoRepository,
            [NotNull] CalcVariableRepository calcVariableRepository, CalcRepo calcRepo)
        {
            _loadTypeDictionary = loadTypeDictionary;
            _availabilityDtoRepository = availabilityDtoRepository;
            _calcVariableRepository = calcVariableRepository;
            _calcRepo = calcRepo;
        }

        [NotNull]
        [ItemNotNull]
        public List<CalcAutoDev> MakeCalcAutoDevs(
            [NotNull][ItemNotNull] List<CalcAutoDevDto> autoDevices,
            [NotNull] DtoCalcLocationDict locationDict)
        {
            var autodevs = new List<CalcAutoDev>(autoDevices.Count);
            foreach (var autoDevDto in autoDevices) {

                List<CalcAutoDevProfile> cadps = new List<CalcAutoDevProfile>();
                foreach (var devProfileDto in autoDevDto.CalcProfiles) {
                    if (_loadTypeDictionary.SimulateLoadtype(devProfileDto.LoadtypeGuid)) {
                        var loadtype = _loadTypeDictionary.GetLoadtypeByGuid(devProfileDto.LoadtypeGuid);
                        CalcProfile cp = MakeCalcProfile(devProfileDto.Profile, _calcRepo.CalcParameters);
                        CalcAutoDevProfile cadp = new CalcAutoDevProfile(cp, loadtype, 1);
                        cadps.Add(cadp);
                    }
                }

                if (cadps.Count == 0) {
                    continue;
                }
                var deviceLoads = MakeCalcDeviceLoads(autoDevDto, _loadTypeDictionary);
                CalcLocation calcLocation = locationDict.GetCalcLocationByGuid(autoDevDto.CalcLocationGuid);
                List<VariableRequirement> requirements = new List<VariableRequirement>();
                foreach(var req in  autoDevDto.Requirements)
                {
                    VariableRequirement vreq = new VariableRequirement(req.Name,
                        req.Value,req.CalcLocationName,req.LocationGuid,
                        req.VariableCondition,_calcVariableRepository,req.VariableGuid);
                    requirements.Add(vreq);
                }

                autoDevDto.AdditionalName = " (autonomous)";
                var cautodev = new CalcAutoDev(cadps,
                    deviceLoads,autoDevDto.TimeStandardDeviation,
                          calcLocation,
                      requirements, autoDevDto,_calcRepo);
                var busyarr = _availabilityDtoRepository.GetByGuid(autoDevDto.BusyArr.Guid);
                foreach (var cadp in cadps) {
                    cautodev.ApplyBitArry(busyarr, cadp.LoadType);
                }
                autodevs.Add(cautodev);
            }
            return autodevs;
        }

        [NotNull]
        public static CalcProfile MakeCalcProfile([NotNull] CalcProfileDto cpd, [NotNull] CalcParameters calcParameters)
        {
            CalcProfile cp = new CalcProfile(cpd.Name, cpd.Guid, calcParameters.InternalStepsize,
                cpd.ProfileType,cpd.DataSource);
            foreach (var dp in cpd.Datapoints) {
                cp.AddNewTimepoint(dp.Time,dp.Value);
            }
            cp.ConvertToTimesteps();
            return cp;
        }

        [NotNull]
        public static CalcProfile GetCalcProfile([NotNull] TimeBasedProfile timeBasedProfile, TimeSpan ts)
        {
            CalcProfile cp = new CalcProfile(timeBasedProfile.Name, Guid.NewGuid().ToStrGuid(),
                ts,(ProfileType)  timeBasedProfile.TimeProfileType, timeBasedProfile.DataSource);
            foreach (var dp in timeBasedProfile.ObservableDatapoints)
            {
                cp.AddNewTimepoint(dp.Time, dp.Value);
            }
            cp.ConvertToTimesteps();
            return cp;
        }

        [NotNull]
        [ItemNotNull]
        public static List<CalcDeviceLoad> MakeCalcDeviceLoads([NotNull] CalcAutoDevDto device,
                                                               [NotNull] CalcLoadTypeDictionary calcLoadTypeDictionary)
        {
            var deviceLoads = new List<CalcDeviceLoad>();
            foreach (var ltdto in device.Loads)
            {
                if (calcLoadTypeDictionary.SimulateLoadtype(ltdto.LoadTypeGuid))
                {
                    var lt = calcLoadTypeDictionary.GetLoadtypeByGuid(ltdto.LoadTypeGuid);
                    var cdl = new CalcDeviceLoad(ltdto.Name,
                        ltdto.MaxPower, lt,
                        ltdto.AverageYearlyConsumption, ltdto.PowerStandardDeviation);
                    deviceLoads.Add(cdl);
                }
            }

            return deviceLoads;
        }

        [NotNull]
        [ItemNotNull]
        public static List<CalcDeviceLoad> MakeCalcDeviceLoads([NotNull] CalcDeviceDto device,
                                                               [NotNull] CalcLoadTypeDictionary calcLoadTypeDictionary)
        {
            var deviceLoads = new List<CalcDeviceLoad>();
            foreach (var ltdto in device.Loads) {
                if (calcLoadTypeDictionary.SimulateLoadtype(ltdto.LoadTypeGuid)) {
                    var lt = calcLoadTypeDictionary.GetLoadtypeByGuid(ltdto.LoadTypeGuid);
                    var cdl = new CalcDeviceLoad(ltdto.Name,
                        ltdto.MaxPower, lt,
                        ltdto.AverageYearlyConsumption, ltdto.PowerStandardDeviation);
                    deviceLoads.Add(cdl);
                }
            }

            return deviceLoads;
        }

        public void MakeCalcDevices([NotNull][ItemNotNull] List<CalcLocation> locs, [NotNull][ItemNotNull] List<CalcDeviceDto> calcDeviceDtos,
                                    [ItemNotNull] [NotNull] List<CalcDevice> calcDevices,
                                    [NotNull] HouseholdKey householdKey, [NotNull] CalcLoadTypeDictionary calcLoadTypeDictionary, CalcRepo calcRepo)
        {
            foreach (var cdd in calcDeviceDtos) {
                var cloc = locs.First(x => x.Guid == cdd.LocationGuid);
                // ggf dev category in dev umwandeln
                var deviceLoads = MakeCalcDeviceLoads(cdd, calcLoadTypeDictionary);

                var cdev = new CalcDevice( deviceLoads,
                     cloc,
                      cdd,calcRepo);

                cloc.Devices.Add(cdev);
                calcDevices.Add(cdev);
            }
        }
        /*
        [CanBeNull]
        private static CalcLocation FindLoc(Location location, IEnumerable<CalcLocation> clocs)
        {
            return clocs.FirstOrDefault(cloc => cloc.ID == location.ID);
        }*/
    }
}