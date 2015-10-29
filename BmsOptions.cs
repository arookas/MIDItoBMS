namespace arookas
{
	class BmsOptions
	{
		// these are fields so I can pass them to ref/out parameters
		public string InputFile;
		public string OutputFile;

		public bool IgnoreMidiBanks;
		public bool IgnoreMidiPrograms;
		public bool IgnoreMidiPitchBends;
		public bool IgnoreMidiExpressions;
		public bool IgnoreMidiVolumes;
		public bool IgnoreMidiPans;
		public bool AddTrackInit;
		public bool SkipPitchRange;
		public bool Batch;

		public BmsTrackDetectionMode TrackDetection = BmsTrackDetectionMode.Auto;

		public double VelocityScale = 1.0d;
		public ushort PerfDuration;
	}

	enum BmsTrackDetectionMode
	{
		Manual,
		Auto,
	}
}
