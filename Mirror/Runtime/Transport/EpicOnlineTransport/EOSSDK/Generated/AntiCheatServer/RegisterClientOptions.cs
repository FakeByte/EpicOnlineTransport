// Copyright Epic Games, Inc. All Rights Reserved.
// This file is automatically generated. Changes to this file may be overwritten.

namespace Epic.OnlineServices.AntiCheatServer
{
	public struct RegisterClientOptions
	{
		/// <summary>
		/// Locally unique value describing the remote user (e.g. a player object pointer)
		/// </summary>
		public System.IntPtr ClientHandle { get; set; }

		/// <summary>
		/// Type of remote user being registered
		/// </summary>
		public AntiCheatCommon.AntiCheatCommonClientType ClientType { get; set; }

		/// <summary>
		/// Remote user's platform, if known
		/// </summary>
		public AntiCheatCommon.AntiCheatCommonClientPlatform ClientPlatform { get; set; }

		/// <summary>
		/// DEPRECATED - New code should set this to null and specify UserId instead.
		/// 
		/// Identifier for the remote user. This is typically a string representation of an
		/// account ID, but it can be any string which is both unique (two different users will never
		/// have the same string) and consistent (if the same user connects to this game session
		/// twice, the same string will be used) in the scope of a single protected game session.
		/// </summary>
		public Utf8String AccountId_DEPRECATED { get; set; }

		/// <summary>
		/// Optional IP address for the remote user. May be null if not available.
		/// IPv4 format: "0.0.0.0"
		/// IPv6 format: "0:0:0:0:0:0:0:0"
		/// </summary>
		public Utf8String IpAddress { get; set; }

		/// <summary>
		/// The Product User ID for the remote user who is being registered.
		/// </summary>
		public ProductUserId UserId { get; set; }
	}

	[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 8)]
	internal struct RegisterClientOptionsInternal : ISettable<RegisterClientOptions>, System.IDisposable
	{
		private int m_ApiVersion;
		private System.IntPtr m_ClientHandle;
		private AntiCheatCommon.AntiCheatCommonClientType m_ClientType;
		private AntiCheatCommon.AntiCheatCommonClientPlatform m_ClientPlatform;
		private System.IntPtr m_AccountId_DEPRECATED;
		private System.IntPtr m_IpAddress;
		private System.IntPtr m_UserId;

		public System.IntPtr ClientHandle
		{
			set
			{
				m_ClientHandle = value;
			}
		}

		public AntiCheatCommon.AntiCheatCommonClientType ClientType
		{
			set
			{
				m_ClientType = value;
			}
		}

		public AntiCheatCommon.AntiCheatCommonClientPlatform ClientPlatform
		{
			set
			{
				m_ClientPlatform = value;
			}
		}

		public Utf8String AccountId_DEPRECATED
		{
			set
			{
				Helper.Set(value, ref m_AccountId_DEPRECATED);
			}
		}

		public Utf8String IpAddress
		{
			set
			{
				Helper.Set(value, ref m_IpAddress);
			}
		}

		public ProductUserId UserId
		{
			set
			{
				Helper.Set(value, ref m_UserId);
			}
		}

		public void Set(ref RegisterClientOptions other)
		{
			m_ApiVersion = AntiCheatServerInterface.RegisterclientApiLatest;
			ClientHandle = other.ClientHandle;
			ClientType = other.ClientType;
			ClientPlatform = other.ClientPlatform;
			AccountId_DEPRECATED = other.AccountId_DEPRECATED;
			IpAddress = other.IpAddress;
			UserId = other.UserId;
		}

		public void Set(ref RegisterClientOptions? other)
		{
			if (other.HasValue)
			{
				m_ApiVersion = AntiCheatServerInterface.RegisterclientApiLatest;
				ClientHandle = other.Value.ClientHandle;
				ClientType = other.Value.ClientType;
				ClientPlatform = other.Value.ClientPlatform;
				AccountId_DEPRECATED = other.Value.AccountId_DEPRECATED;
				IpAddress = other.Value.IpAddress;
				UserId = other.Value.UserId;
			}
		}

		public void Dispose()
		{
			Helper.Dispose(ref m_ClientHandle);
			Helper.Dispose(ref m_AccountId_DEPRECATED);
			Helper.Dispose(ref m_IpAddress);
			Helper.Dispose(ref m_UserId);
		}
	}
}