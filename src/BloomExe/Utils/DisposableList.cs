using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bloom.Utils
{
	internal class DisposableList<T> : List<T>, IDisposable  where T: IDisposable
	{
		private bool disposedValue;

		public DisposableList()
			: base()
		{
		}

		protected virtual void Dispose(bool disposing)
		{
			// TODO: This class is not tested in the slightest.

			if (!disposedValue)
			{
				if (disposing)
				{
					// dispose managed state (managed objects)
					for (int i = 0; i < this.Count; ++i)
					{
						this[i].Dispose();
					}
				}

				// free unmanaged resources (unmanaged objects) and override finalizer
				// set large fields to null
				//
				// Nothing to do.


				disposedValue = true;
			}
		}

		// // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
		// ~DisposableList()
		// {
		//     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		//     Dispose(disposing: false);
		// }

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
