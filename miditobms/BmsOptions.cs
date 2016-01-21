namespace arookas {
	class BmsOptions {
		// these are fields so I can pass them to ref/out parameters
		public string mInputFile;
		public string mOutputFile;

		public bool mIgnoreBanks;
		public bool mIgnorePrograms;
		public bool mIgnorePitchBends;
		public bool mIgnoreExpressions;
		public bool mIgnoreVolumes;
		public bool mIgnorePans;
		public bool mAddTrackInit;
		public bool mSkipPitchRange;
		public bool mBatch;

		public BmsTrackDetectionMode mTrackMode = BmsTrackDetectionMode.Auto;

		public double mVelScale = 1.0d;
		public ushort mPerfDuration;
	}

	enum BmsTrackDetectionMode {
		Manual,
		Auto,
	}
}
