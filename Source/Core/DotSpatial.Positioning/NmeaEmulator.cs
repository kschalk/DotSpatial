﻿// Copyright (c) DotSpatial Team. All rights reserved.
// Licensed under the MIT, license. See License.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace DotSpatial.Positioning
{
    /// <summary>
    /// Emultor for MNEA devices
    /// </summary>
    public class NmeaEmulator : Emulator
    {
        /// <summary>
        ///
        /// </summary>
        private DateTime _gpggaLastSent = DateTime.UtcNow;
        /// <summary>
        ///
        /// </summary>
        private DateTime _gpgsaLastSent = DateTime.UtcNow;
        /// <summary>
        ///
        /// </summary>
        private DateTime _gpgllLastSent = DateTime.UtcNow;
        /// <summary>
        ///
        /// </summary>
        private DateTime _gpgsvLastSent = DateTime.UtcNow;
        /// <summary>
        ///
        /// </summary>
        private DateTime _gprmcLastSent = DateTime.UtcNow;
        /// <summary>
        ///
        /// </summary>
        private TimeSpan _gpggaInterval = TimeSpan.FromSeconds(1);
        /// <summary>
        ///
        /// </summary>
        private TimeSpan _gpgsaInterval = TimeSpan.FromSeconds(1);
        /// <summary>
        ///
        /// </summary>
        private TimeSpan _gpgllInterval = TimeSpan.FromSeconds(1);
        /// <summary>
        ///
        /// </summary>
        private TimeSpan _gpgsvInterval = TimeSpan.FromSeconds(5);
        /// <summary>
        ///
        /// </summary>
        private TimeSpan _gprmcInterval = TimeSpan.FromSeconds(1);

        // Emulation settings
        /// <summary>
        ///
        /// </summary>
        private DilutionOfPrecision _horizontalDOP = DilutionOfPrecision.Good;
        /// <summary>
        ///
        /// </summary>
        private DilutionOfPrecision _verticalDOP = DilutionOfPrecision.Good;
        /// <summary>
        ///
        /// </summary>
        private DilutionOfPrecision _meanDOP = DilutionOfPrecision.Good;

        /// <summary>
        ///
        /// </summary>
        private Longitude _magneticVariation = new(1.0);

        // Random emulation variables
        /// <summary>
        ///
        /// </summary>
        private double _minHdop = 1;
        /// <summary>
        ///
        /// </summary>
        private double _maxHdop = 6;
        /// <summary>
        ///
        /// </summary>
        private double _minVdop = 1;
        /// <summary>
        ///
        /// </summary>
        private double _maxVdop = 6;

        /// <summary>
        /// Creates a generic NMEA-0183 Emulator
        /// </summary>
        public NmeaEmulator()
            : this("Generic NMEA-0183 Emulator (http://dotspatial.codeplex.com)")
        { }

        /// <summary>
        /// Creates a generic NMEA-0183 Emulator from the specified string name
        /// </summary>
        /// <param name="name">The name.</param>
        public NmeaEmulator(string name)
            : base(name)
        { }

        ///// <summary>
        ///// Copies the settings of the NMEA Emulator.
        ///// </summary>
        ///// <returns> A new NMEA Emulator with the same settings. </returns>
        // public override Emulator Clone()
        //{
        //    // Make a new emulator
        //    NmeaEmulator emulator = (NmeaEmulator)Clone(new NmeaEmulator());

        //    emulator._HorizontalDOP = _HorizontalDOP;
        //    emulator._VerticalDOP = _VerticalDOP;
        //    emulator._MeanDOP = _MeanDOP;
        //    emulator._FixMode = _FixMode;
        //    emulator._FixStatus = _FixStatus;
        //    emulator._FixMethod = _FixMethod;
        //    emulator._FixQuality = _FixQuality;
        //    emulator._MagneticVariation = _MagneticVariation;

        //    emulator._minHDOP = _minHDOP;
        //    emulator._maxHDOP = _maxHDOP;
        //    emulator._minVDOP = _minVDOP;
        //    emulator._maxVDOP = _maxVDOP;

        //    return emulator;
        //}

        /// <summary>
        /// Sets the update intervals for the NMEA Emulator's sentence generation.
        /// </summary>
        /// <value>The interval.</value>
        public override TimeSpan Interval
        {
            get => base.Interval;
            set
            {
                base.Interval = value;
                _gpggaInterval = _gpgsaInterval = _gpgllInterval = _gprmcInterval = value;
                _gpgsvInterval = TimeSpan.FromMilliseconds(value.TotalMilliseconds * 5);
            }
        }

        /// <summary>
        /// Randomizes the emulation by changing speed and direction
        /// </summary>
        public override void Randomize()
        {
            // Randomize the base emulation for speed/bearing
            base.Randomize();

            _horizontalDOP = new DilutionOfPrecision((float)(Seed.NextDouble() * (_maxHdop - _minHdop) + _minHdop));
            _verticalDOP = new DilutionOfPrecision((float)(Seed.NextDouble() * (_maxVdop - _minVdop) + _minVdop));

            // Mean is hypotenuse of the (X, Y, Z, n) axes.
            _meanDOP = new DilutionOfPrecision((float)Math.Sqrt(Math.Pow(_horizontalDOP.Value, 2) + Math.Pow(_verticalDOP.Value, 2)));

            lock (Satellites)
            {
                if (Satellites.Count == 0)
                {
                    int sats = Seed.Next(4, 12);

                    // Satellites.Add(new Satellite(32, new Azimuth(225), new Elevation(45), new SignalToNoiseRatio(25)));

                    Satellites.Add(new Satellite(32, new Azimuth(Seed.Next(360)), new Elevation(Seed.Next(90)), new SignalToNoiseRatio(Seed.Next(50))));
                    if (sats > 1)
                    {
                        Satellites.Add(new Satellite(24, new Azimuth(Seed.Next(360)), new Elevation(Seed.Next(90)), new SignalToNoiseRatio(Seed.Next(50))));
                    }

                    if (sats > 2)
                    {
                        Satellites.Add(new Satellite(25, new Azimuth(Seed.Next(360)), new Elevation(Seed.Next(90)), new SignalToNoiseRatio(Seed.Next(50))));
                    }

                    if (sats > 3)
                    {
                        Satellites.Add(new Satellite(26, new Azimuth(Seed.Next(360)), new Elevation(Seed.Next(90)), new SignalToNoiseRatio(Seed.Next(50))));
                    }

                    if (sats > 4)
                    {
                        Satellites.Add(new Satellite(27, new Azimuth(Seed.Next(360)), new Elevation(Seed.Next(90)), new SignalToNoiseRatio(Seed.Next(50))));
                    }

                    if (sats > 5)
                    {
                        Satellites.Add(new Satellite(16, new Azimuth(Seed.Next(360)), new Elevation(Seed.Next(90)), new SignalToNoiseRatio(Seed.Next(50))));
                    }

                    if (sats > 6)
                    {
                        Satellites.Add(new Satellite(14, new Azimuth(Seed.Next(360)), new Elevation(Seed.Next(90)), new SignalToNoiseRatio(Seed.Next(50))));
                    }

                    if (sats > 7)
                    {
                        Satellites.Add(new Satellite(6, new Azimuth(Seed.Next(360)), new Elevation(Seed.Next(90)), new SignalToNoiseRatio(Seed.Next(50))));
                    }

                    if (sats > 8)
                    {
                        Satellites.Add(new Satellite(7, new Azimuth(Seed.Next(360)), new Elevation(Seed.Next(90)), new SignalToNoiseRatio(Seed.Next(50))));
                    }

                    if (sats > 9)
                    {
                        Satellites.Add(new Satellite(4, new Azimuth(Seed.Next(360)), new Elevation(Seed.Next(90)), new SignalToNoiseRatio(Seed.Next(50))));
                    }

                    if (sats > 10)
                    {
                        Satellites.Add(new Satellite(19, new Azimuth(Seed.Next(360)), new Elevation(Seed.Next(90)), new SignalToNoiseRatio(Seed.Next(50))));
                    }

                    if (sats > 11)
                    {
                        Satellites.Add(new Satellite(8, new Azimuth(Seed.Next(360)), new Elevation(Seed.Next(90)), new SignalToNoiseRatio(Seed.Next(50))));
                    }
                }
            }

            SetRandom(true);
        }

        /// <summary>
        /// Randomize
        /// </summary>
        /// <param name="maxHDOP">The max HDOP.</param>
        /// <param name="maxVDOP">The max VDOP.</param>
        public void Randomize(DilutionOfPrecision maxHDOP, DilutionOfPrecision maxVDOP)
        {
            _minHdop = 1;
            _maxHdop = maxHDOP.Value;
            _minVdop = 1;
            _maxVdop = maxVDOP.Value;

            SetRandom(true);
        }

        /// <summary>
        /// Randomize
        /// </summary>
        /// <param name="minHDOP">The min HDOP.</param>
        /// <param name="maxHDOP">The max HDOP.</param>
        /// <param name="minVDOP">The min VDOP.</param>
        /// <param name="maxVDOP">The max VDOP.</param>
        public void Randomize(DilutionOfPrecision minHDOP, DilutionOfPrecision maxHDOP, DilutionOfPrecision minVDOP, DilutionOfPrecision maxVDOP)
        {
            _minHdop = minHDOP.Value;
            _maxHdop = maxHDOP.Value;
            _minVdop = minVDOP.Value;
            _maxVdop = maxVDOP.Value;

            SetRandom(true);
        }

        /// <summary>
        /// Randomize
        /// </summary>
        /// <param name="seed">The seed.</param>
        /// <param name="speedLow">The speed low.</param>
        /// <param name="speedHigh">The speed high.</param>
        /// <param name="bearingStart">The bearing start.</param>
        /// <param name="bearingArc">The bearing arc.</param>
        /// <param name="minHDOP">The min HDOP.</param>
        /// <param name="maxHDOP">The max HDOP.</param>
        /// <param name="minVDOP">The min VDOP.</param>
        /// <param name="maxVDOP">The max VDOP.</param>
        public void Randomize(Random seed, Speed speedLow, Speed speedHigh, Azimuth bearingStart, Azimuth bearingArc, DilutionOfPrecision minHDOP,
                              DilutionOfPrecision maxHDOP, DilutionOfPrecision minVDOP, DilutionOfPrecision maxVDOP)
        {
            Randomize(seed, speedLow, speedHigh, bearingStart, bearingArc);

            _minHdop = minHDOP.Value;
            _maxHdop = maxHDOP.Value;
            _minVdop = minVDOP.Value;
            _maxVdop = maxVDOP.Value;

            SetRandom(true);
        }

        /// <summary>
        /// Generates actual data to send to the client.
        /// </summary>
        /// <remarks>Data is sent according to the behavior of a typical GPS device: $GPGGA,
        /// $GPGSA, $GPRMC, $GPGSV sentences are sent every second, and a $GPGSV sentence
        /// is sent every five seconds.
        /// Developers who want to emulate a specific model of GPS device should override this
        /// method and generate the sentences specific to that device.</remarks>
        protected override void OnEmulation()
        {
            // Update real-time position, speed, bearing, etc.
            base.OnEmulation();

            if (Route.Count == 0)
            {
                CurrentPosition = EmulatePositionError(CurrentPosition);
            }

            /* NMEA devices will transmit "bursts" of NMEA sentences, followed by a one-second pause.
             * Other sentences (usually $GPGSV) are transmitted once every few seconds.  This emulator,
             * by default, will transmit the most common NMEA sentences.
             */

            // $GPGGA
            if (!_gpggaInterval.Equals(TimeSpan.Zero)
                // Has enough time elapsed to send the sentence?
                && UtcDateTime.Subtract(_gpggaLastSent) > _gpggaInterval)
            {
                // Get the tracked satellite count
                int trackedCount = Satellites.Count(item => item.SignalToNoiseRatio.Value > 0);

                // Yes
                _gpggaLastSent = UtcDateTime;

                // Queue the sentence to the read buffer
                WriteSentenceToClient(new GpggaSentence(UtcDateTime.TimeOfDay, CurrentPosition, EmulatedFixQuality, trackedCount,
                    _horizontalDOP, Altitude.Add(EmulateError(_verticalDOP)), Distance.Empty, TimeSpan.Zero, -1)); // Add an error to the altitude written to the client but don't change the actual value (otherwise it will "walk")
            }

            // $GPRMC
            if (!_gprmcInterval.Equals(TimeSpan.Zero)
                // Has enough time elapsed to send the sentence?
                && UtcDateTime.Subtract(_gprmcLastSent) > _gprmcInterval)
            {
                // Yes
                _gprmcLastSent = UtcDateTime;

                // Queue the sentence to the read buffer
                WriteSentenceToClient(new GprmcSentence(UtcDateTime, EmulatedFixStatus == FixStatus.Fix, CurrentPosition, Speed,
                    Bearing, _magneticVariation));
            }

            // $GPGLL
            if (!_gpgllInterval.Equals(TimeSpan.Zero)
                // Has enough time elapsed to send the sentence?
                && UtcDateTime.Subtract(_gpgllLastSent) > _gpgllInterval)
            {
                // Yes
                _gpgllLastSent = UtcDateTime;

                // Write a $GPGLL to the client
                WriteSentenceToClient(new GpgllSentence(CurrentPosition, UtcDateTime.TimeOfDay, EmulatedFixStatus));
            }

            // $GPGSA
            if (!_gpgsaInterval.Equals(TimeSpan.Zero)
                // Has enough time elapsed to send the sentence?
                && UtcDateTime.Subtract(_gpgsaLastSent) > _gpgsaInterval)
            {
                // Yes
                _gpgsaLastSent = UtcDateTime;

                // Queue the sentence to the read buffer
                WriteSentenceToClient(new GpgsaSentence(EmulatedFixMode, EmulatedFixMethod, Satellites,
                    _meanDOP, _horizontalDOP, _verticalDOP));
            }

            // $GPGSV
            if (!_gpgsvInterval.Equals(TimeSpan.Zero)
                // Has enough time elapsed to send the sentence?
                && UtcDateTime.Subtract(_gpgsvLastSent) > _gpgsvInterval)
            {
                // Build a list of sentences from our satellites
                IList<GpgsvSentence> sentences = GpgsvSentence.FromSatellites(Satellites);

                // Yes
                _gpgsvLastSent = UtcDateTime;

                // Write each sentence to the read buffer
                foreach (GpgsvSentence gpgsv in sentences)
                {
                    WriteSentenceToClient(gpgsv);
                }
            }

            // And signal that we have data (or not)
            if (ReadBuffer.Count == 0)
            {
                ReadDataAvailableWaitHandle.Reset();
            }
            else
            {
                ReadDataAvailableWaitHandle.Set();
            }
        }

        /// <summary>
        /// Emulates the position error.
        /// </summary>
        /// <param name="truth">The truth.</param>
        /// <returns></returns>
        protected virtual Position EmulatePositionError(Position truth)
        {
            // Introduce the error
            return truth.TranslateTo(new Angle(Seed.NextDouble() * 360), EmulateError(_horizontalDOP));
        }

        /// <summary>
        /// Emulates the error.
        /// </summary>
        /// <param name="dop">The dop.</param>
        /// <returns></returns>
        protected virtual Distance EmulateError(DilutionOfPrecision dop)
        {
            // Calculate the error variance
            // return Distance.FromMeters((Seed.NextDouble() * dop.Value) + DilutionOfPrecision.CurrentAverageDevicePrecision.ToMeters().Value); really? isn't that what the estimated precision is for and shouldn't it be +/- the estimated precision range divided by 2 as below
            return Distance.FromMeters(dop.EstimatedPrecision.ToMeters().Value * (Seed.NextDouble() - 0.5));
        }

        /// <summary>
        /// Writes the sentence to client.
        /// </summary>
        /// <param name="sentence">The sentence.</param>
        protected void WriteSentenceToClient(NmeaSentence sentence)
        {
            // Get a byte array of the sentence
            byte[] sentenceBytes = sentence.ToByteArray();

            /* Some customers were found to make an emulator, but then not actually read from it.
             * To prevent huge buffers, we will only write a sentence if the buffer can handle it.
             * Otherwise, we'll ignore it completely.
             */
            if (ReadBuffer.Count + sentenceBytes.Length + 2 > ReadBuffer.Capacity)
            {
                return;
            }

            // Add the bytes
            ReadBuffer.AddRange(sentenceBytes);

            // Add a CrLf
            ReadBuffer.Add(13);
            ReadBuffer.Add(10);
        }

        /// <summary>
        /// Horizontal dilution of precision
        /// </summary>
        /// <value>The horizontal dilution of precision.</value>
        public DilutionOfPrecision HorizontalDilutiuonOfPrecision
        {
            get => _horizontalDOP;
            set
            {
                _horizontalDOP = value;
                SetRandom(false);
            }
        }

        /// <summary>
        /// Vertical dilution of precision
        /// </summary>
        /// <value>The vertical dilution of precision.</value>
        public DilutionOfPrecision VerticalDilutiuonOfPrecision
        {
            get => _verticalDOP;
            set
            {
                _verticalDOP = value;
                SetRandom(false);
            }
        }

        /// <summary>
        /// Mean Dilution of Precision
        /// </summary>
        /// <value>The mean dilution of precision.</value>
        public DilutionOfPrecision MeanDilutiuonOfPrecision
        {
            get => _meanDOP;
            set
            {
                _meanDOP = value;
                SetRandom(false);
            }
        }

        /// <summary>
        /// Emulated Fix Mode
        /// </summary>
        /// <value>The emulated fix mode.</value>
        public FixMode EmulatedFixMode { get; set; } = FixMode.Automatic;

        /// <summary>
        /// Emulated Fix Status
        /// </summary>
        /// <value>The emulated fix status.</value>
        public FixStatus EmulatedFixStatus { get; set; } = FixStatus.Fix;

        /// <summary>
        /// Emulated Fix Method
        /// </summary>
        /// <value>The emulated fix method.</value>
        public FixMethod EmulatedFixMethod { get; set; } = FixMethod.Fix3D;

        /// <summary>
        /// Emulated Fix Quality
        /// </summary>
        /// <value>The emulated fix quality.</value>
        public FixQuality EmulatedFixQuality { get; set; } = FixQuality.Simulated;

        /// <summary>
        /// Magnetic Variation
        /// </summary>
        /// <value>The magnetic variation.</value>
        public Longitude MagneticVariation
        {
            get => _magneticVariation;
            set => _magneticVariation = value;
        }
    }
}