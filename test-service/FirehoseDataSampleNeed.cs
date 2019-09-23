using System;

namespace testService
{
	public class FirehoseDataSampleNeed
	{
		public int Number { get; set; } = new Random().Next(0, int.MaxValue);
		public string Word { get; set; } = Guid.NewGuid().ToString();
	}
}