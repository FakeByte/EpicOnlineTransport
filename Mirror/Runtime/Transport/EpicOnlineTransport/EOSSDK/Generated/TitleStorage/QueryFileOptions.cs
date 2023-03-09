// Copyright Epic Games, Inc. All Rights Reserved.
// This file is automatically generated. Changes to this file may be overwritten.

namespace Epic.OnlineServices.TitleStorage
{
	/// <summary>
	/// Input data for the <see cref="TitleStorageInterface.QueryFile" /> function
	/// </summary>
	public struct QueryFileOptions
	{
		/// <summary>
		/// Product User ID of the local user requesting file metadata (optional)
		/// </summary>
		public ProductUserId LocalUserId { get; set; }

		/// <summary>
		/// The requested file's name
		/// </summary>
		public Utf8String Filename { get; set; }
	}

	[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 8)]
	internal struct QueryFileOptionsInternal : ISettable<QueryFileOptions>, System.IDisposable
	{
		private int m_ApiVersion;
		private System.IntPtr m_LocalUserId;
		private System.IntPtr m_Filename;

		public ProductUserId LocalUserId
		{
			set
			{
				Helper.Set(value, ref m_LocalUserId);
			}
		}

		public Utf8String Filename
		{
			set
			{
				Helper.Set(value, ref m_Filename);
			}
		}

		public void Set(ref QueryFileOptions other)
		{
			m_ApiVersion = TitleStorageInterface.QueryfileApiLatest;
			LocalUserId = other.LocalUserId;
			Filename = other.Filename;
		}

		public void Set(ref QueryFileOptions? other)
		{
			if (other.HasValue)
			{
				m_ApiVersion = TitleStorageInterface.QueryfileApiLatest;
				LocalUserId = other.Value.LocalUserId;
				Filename = other.Value.Filename;
			}
		}

		public void Dispose()
		{
			Helper.Dispose(ref m_LocalUserId);
			Helper.Dispose(ref m_Filename);
		}
	}
}