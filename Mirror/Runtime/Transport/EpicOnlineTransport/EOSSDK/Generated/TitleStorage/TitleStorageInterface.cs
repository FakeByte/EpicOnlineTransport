// Copyright Epic Games, Inc. All Rights Reserved.
// This file is automatically generated. Changes to this file may be overwritten.

namespace Epic.OnlineServices.TitleStorage
{
	public sealed partial class TitleStorageInterface : Handle
	{
		public TitleStorageInterface()
		{
		}

		public TitleStorageInterface(System.IntPtr innerHandle) : base(innerHandle)
		{
		}

		/// <summary>
		/// The most recent version of the <see cref="CopyFileMetadataAtIndex" /> API.
		/// </summary>
		public const int CopyfilemetadataatindexApiLatest = 1;

		/// <summary>
		/// DEPRECATED! Use <see cref="CopyfilemetadataatindexApiLatest" /> instead.
		/// </summary>
		public const int CopyfilemetadataatindexoptionsApiLatest = CopyfilemetadataatindexApiLatest;

		/// <summary>
		/// The most recent version of the <see cref="CopyFileMetadataByFilename" /> API.
		/// </summary>
		public const int CopyfilemetadatabyfilenameApiLatest = 1;

		/// <summary>
		/// DEPRECATED! Use <see cref="CopyfilemetadatabyfilenameApiLatest" /> instead.
		/// </summary>
		public const int CopyfilemetadatabyfilenameoptionsApiLatest = CopyfilemetadatabyfilenameApiLatest;

		/// <summary>
		/// The most recent version of the <see cref="DeleteCache" /> API.
		/// </summary>
		public const int DeletecacheApiLatest = 1;

		/// <summary>
		/// DEPRECATED! Use <see cref="DeletecacheApiLatest" /> instead.
		/// </summary>
		public const int DeletecacheoptionsApiLatest = DeletecacheApiLatest;

		/// <summary>
		/// The most recent version of the <see cref="FileMetadata" /> API.
		/// </summary>
		public const int FilemetadataApiLatest = 2;

		/// <summary>
		/// Maximum File Name Length in bytes
		/// </summary>
		public const int FilenameMaxLengthBytes = 64;

		/// <summary>
		/// The most recent version of the <see cref="GetFileMetadataCount" /> API.
		/// </summary>
		public const int GetfilemetadatacountApiLatest = 1;

		/// <summary>
		/// DEPRECATED! Use <see cref="GetfilemetadatacountApiLatest" /> instead.
		/// </summary>
		public const int GetfilemetadatacountoptionsApiLatest = GetfilemetadatacountApiLatest;

		/// <summary>
		/// The most recent version of the <see cref="QueryFile" /> API.
		/// </summary>
		public const int QueryfileApiLatest = 1;

		/// <summary>
		/// The most recent version of the <see cref="QueryFileList" /> API.
		/// </summary>
		public const int QueryfilelistApiLatest = 1;

		/// <summary>
		/// DEPRECATED! Use <see cref="QueryfilelistApiLatest" /> instead.
		/// </summary>
		public const int QueryfilelistoptionsApiLatest = QueryfilelistApiLatest;

		/// <summary>
		/// DEPRECATED! Use <see cref="QueryfileApiLatest" /> instead.
		/// </summary>
		public const int QueryfileoptionsApiLatest = QueryfileApiLatest;

		/// <summary>
		/// The most recent version of the <see cref="ReadFile" /> API.
		/// </summary>
		public const int ReadfileApiLatest = 1;

		/// <summary>
		/// DEPRECATED! Use <see cref="ReadfileApiLatest" /> instead.
		/// </summary>
		public const int ReadfileoptionsApiLatest = ReadfileApiLatest;

		/// <summary>
		/// Get the cached copy of a file's metadata by index. The metadata will be for the last retrieved version. The returned pointer must be released by the user when no longer needed.
		/// <seealso cref="GetFileMetadataCount" />
		/// <seealso cref="Release" />
		/// </summary>
		/// <param name="options">Object containing properties related to which user is requesting metadata, and at what index</param>
		/// <param name="outMetadata">A copy of the FileMetadata structure will be set if successful. This data must be released by calling <see cref="Release" />.</param>
		/// <returns>
		/// <see cref="Result.Success" /> if the requested metadata is currently cached, otherwise an error result explaining what went wrong.
		/// </returns>
		public Result CopyFileMetadataAtIndex(ref CopyFileMetadataAtIndexOptions options, out FileMetadata? outMetadata)
		{
			CopyFileMetadataAtIndexOptionsInternal optionsInternal = new CopyFileMetadataAtIndexOptionsInternal();
			optionsInternal.Set(ref options);

			var outMetadataAddress = System.IntPtr.Zero;

			var funcResult = Bindings.EOS_TitleStorage_CopyFileMetadataAtIndex(InnerHandle, ref optionsInternal, ref outMetadataAddress);

			Helper.Dispose(ref optionsInternal);

			Helper.Get<FileMetadataInternal, FileMetadata>(outMetadataAddress, out outMetadata);
			if (outMetadata != null)
			{
				Bindings.EOS_TitleStorage_FileMetadata_Release(outMetadataAddress);
			}

			return funcResult;
		}

		/// <summary>
		/// Create a cached copy of a file's metadata by filename. The metadata will be for the last retrieved or successfully saved version, and will not include any changes that have not
		/// completed writing. The returned pointer must be released by the user when no longer needed.
		/// </summary>
		/// <param name="options">Object containing properties related to which user is requesting metadata, and for which filename</param>
		/// <param name="outMetadata">A copy of the FileMetadata structure will be set if successful. This data must be released by calling <see cref="Release" />.</param>
		/// <returns>
		/// <see cref="Result.Success" /> if the metadata is currently cached, otherwise an error result explaining what went wrong
		/// </returns>
		public Result CopyFileMetadataByFilename(ref CopyFileMetadataByFilenameOptions options, out FileMetadata? outMetadata)
		{
			CopyFileMetadataByFilenameOptionsInternal optionsInternal = new CopyFileMetadataByFilenameOptionsInternal();
			optionsInternal.Set(ref options);

			var outMetadataAddress = System.IntPtr.Zero;

			var funcResult = Bindings.EOS_TitleStorage_CopyFileMetadataByFilename(InnerHandle, ref optionsInternal, ref outMetadataAddress);

			Helper.Dispose(ref optionsInternal);

			Helper.Get<FileMetadataInternal, FileMetadata>(outMetadataAddress, out outMetadata);
			if (outMetadata != null)
			{
				Bindings.EOS_TitleStorage_FileMetadata_Release(outMetadataAddress);
			}

			return funcResult;
		}

		/// <summary>
		/// Clear previously cached file data. This operation will be done asynchronously. All cached files except those corresponding to the transfers in progress will be removed.
		/// Warning: Use this with care. Cache system generally tries to clear old and unused cached files from time to time. Unnecessarily clearing cache can degrade performance as SDK will have to re-download data.
		/// </summary>
		/// <param name="options">Object containing properties related to which user is deleting cache</param>
		/// <param name="clientData">Optional pointer to help clients track this request, that is returned in associated callbacks</param>
		/// <param name="completionCallback">This function is called when the delete cache operation completes</param>
		/// <returns>
		/// <see cref="Result.Success" /> if the operation was started correctly, otherwise an error result explaining what went wrong
		/// </returns>
		public Result DeleteCache(ref DeleteCacheOptions options, object clientData, OnDeleteCacheCompleteCallback completionCallback)
		{
			DeleteCacheOptionsInternal optionsInternal = new DeleteCacheOptionsInternal();
			optionsInternal.Set(ref options);

			var clientDataAddress = System.IntPtr.Zero;

			var completionCallbackInternal = new OnDeleteCacheCompleteCallbackInternal(OnDeleteCacheCompleteCallbackInternalImplementation);
			Helper.AddCallback(out clientDataAddress, clientData, completionCallback, completionCallbackInternal);

			var funcResult = Bindings.EOS_TitleStorage_DeleteCache(InnerHandle, ref optionsInternal, clientDataAddress, completionCallbackInternal);

			Helper.Dispose(ref optionsInternal);

			return funcResult;
		}

		/// <summary>
		/// Get the count of files we have previously queried information for and files we have previously read from / written to.
		/// <seealso cref="CopyFileMetadataAtIndex" />
		/// </summary>
		/// <param name="options">Object containing properties related to which user is requesting the metadata count</param>
		/// <returns>
		/// If successful, the count of metadata currently cached. Returns 0 on failure.
		/// </returns>
		public uint GetFileMetadataCount(ref GetFileMetadataCountOptions options)
		{
			GetFileMetadataCountOptionsInternal optionsInternal = new GetFileMetadataCountOptionsInternal();
			optionsInternal.Set(ref options);

			var funcResult = Bindings.EOS_TitleStorage_GetFileMetadataCount(InnerHandle, ref optionsInternal);

			Helper.Dispose(ref optionsInternal);

			return funcResult;
		}

		/// <summary>
		/// Query a specific file's metadata, such as file names, size, and a MD5 hash of the data. This is not required before a file may be opened. Once a file has
		/// been queried, its metadata will be available by the <see cref="CopyFileMetadataAtIndex" /> and <see cref="CopyFileMetadataByFilename" /> functions.
		/// <seealso cref="GetFileMetadataCount" />
		/// <seealso cref="CopyFileMetadataAtIndex" />
		/// <seealso cref="CopyFileMetadataByFilename" />
		/// </summary>
		/// <param name="options">Object containing properties related to which user is querying files, and what file is being queried</param>
		/// <param name="clientData">Optional pointer to help clients track this request, that is returned in the completion callback</param>
		/// <param name="completionCallback">This function is called when the query operation completes</param>
		public void QueryFile(ref QueryFileOptions options, object clientData, OnQueryFileCompleteCallback completionCallback)
		{
			QueryFileOptionsInternal optionsInternal = new QueryFileOptionsInternal();
			optionsInternal.Set(ref options);

			var clientDataAddress = System.IntPtr.Zero;

			var completionCallbackInternal = new OnQueryFileCompleteCallbackInternal(OnQueryFileCompleteCallbackInternalImplementation);
			Helper.AddCallback(out clientDataAddress, clientData, completionCallback, completionCallbackInternal);

			Bindings.EOS_TitleStorage_QueryFile(InnerHandle, ref optionsInternal, clientDataAddress, completionCallbackInternal);

			Helper.Dispose(ref optionsInternal);
		}

		/// <summary>
		/// Query the file metadata, such as file names, size, and a MD5 hash of the data, for all files available for current user based on their settings (such as game role) and tags provided.
		/// This is not required before a file can be downloaded by name.
		/// </summary>
		/// <param name="options">Object containing properties related to which user is querying files and the list of tags</param>
		/// <param name="clientData">Optional pointer to help clients track this request, that is returned in the completion callback</param>
		/// <param name="completionCallback">This function is called when the query operation completes</param>
		public void QueryFileList(ref QueryFileListOptions options, object clientData, OnQueryFileListCompleteCallback completionCallback)
		{
			QueryFileListOptionsInternal optionsInternal = new QueryFileListOptionsInternal();
			optionsInternal.Set(ref options);

			var clientDataAddress = System.IntPtr.Zero;

			var completionCallbackInternal = new OnQueryFileListCompleteCallbackInternal(OnQueryFileListCompleteCallbackInternalImplementation);
			Helper.AddCallback(out clientDataAddress, clientData, completionCallback, completionCallbackInternal);

			Bindings.EOS_TitleStorage_QueryFileList(InnerHandle, ref optionsInternal, clientDataAddress, completionCallbackInternal);

			Helper.Dispose(ref optionsInternal);
		}

		/// <summary>
		/// Retrieve the contents of a specific file, potentially downloading the contents if we do not have a local copy, from the cloud. This request will occur asynchronously, potentially over
		/// multiple frames. All callbacks for this function will come from the same thread that the SDK is ticked from. If specified, the FileTransferProgressCallback will always be called at
		/// least once if the request is started successfully.
		/// <seealso cref="TitleStorageFileTransferRequest.Release" />
		/// </summary>
		/// <param name="options">Object containing properties related to which user is opening the file, what the file's name is, and related mechanisms for copying the data</param>
		/// <param name="clientData">Optional pointer to help clients track this request, that is returned in associated callbacks</param>
		/// <param name="completionCallback">This function is called when the read operation completes</param>
		/// <returns>
		/// A valid Title Storage File Request handle if successful, or <see langword="null" /> otherwise. Data contained in the completion callback will have more detailed information about issues with the request in failure cases. This handle must be released when it is no longer needed
		/// </returns>
		public TitleStorageFileTransferRequest ReadFile(ref ReadFileOptions options, object clientData, OnReadFileCompleteCallback completionCallback)
		{
			ReadFileOptionsInternal optionsInternal = new ReadFileOptionsInternal();
			optionsInternal.Set(ref options);

			var clientDataAddress = System.IntPtr.Zero;

			var completionCallbackInternal = new OnReadFileCompleteCallbackInternal(OnReadFileCompleteCallbackInternalImplementation);
			Helper.AddCallback(out clientDataAddress, clientData, completionCallback, completionCallbackInternal, options.ReadFileDataCallback, ReadFileOptionsInternal.ReadFileDataCallback, options.FileTransferProgressCallback, ReadFileOptionsInternal.FileTransferProgressCallback);

			var funcResult = Bindings.EOS_TitleStorage_ReadFile(InnerHandle, ref optionsInternal, clientDataAddress, completionCallbackInternal);

			Helper.Dispose(ref optionsInternal);

			TitleStorageFileTransferRequest funcResultReturn;
			Helper.Get(funcResult, out funcResultReturn);
			return funcResultReturn;
		}

		[MonoPInvokeCallback(typeof(OnDeleteCacheCompleteCallbackInternal))]
		internal static void OnDeleteCacheCompleteCallbackInternalImplementation(ref DeleteCacheCallbackInfoInternal data)
		{
			OnDeleteCacheCompleteCallback callback;
			DeleteCacheCallbackInfo callbackInfo;
			if (Helper.TryGetAndRemoveCallback(ref data, out callback, out callbackInfo))
			{
				callback(ref callbackInfo);
			}
		}

		[MonoPInvokeCallback(typeof(OnFileTransferProgressCallbackInternal))]
		internal static void OnFileTransferProgressCallbackInternalImplementation(ref FileTransferProgressCallbackInfoInternal data)
		{
			OnFileTransferProgressCallback callback;
			FileTransferProgressCallbackInfo callbackInfo;
			if (Helper.TryGetStructCallback(ref data, out callback, out callbackInfo))
			{
				FileTransferProgressCallbackInfo dataObj;
				Helper.Get(ref data, out dataObj);

				callback(ref dataObj);
			}
		}

		[MonoPInvokeCallback(typeof(OnQueryFileCompleteCallbackInternal))]
		internal static void OnQueryFileCompleteCallbackInternalImplementation(ref QueryFileCallbackInfoInternal data)
		{
			OnQueryFileCompleteCallback callback;
			QueryFileCallbackInfo callbackInfo;
			if (Helper.TryGetAndRemoveCallback(ref data, out callback, out callbackInfo))
			{
				callback(ref callbackInfo);
			}
		}

		[MonoPInvokeCallback(typeof(OnQueryFileListCompleteCallbackInternal))]
		internal static void OnQueryFileListCompleteCallbackInternalImplementation(ref QueryFileListCallbackInfoInternal data)
		{
			OnQueryFileListCompleteCallback callback;
			QueryFileListCallbackInfo callbackInfo;
			if (Helper.TryGetAndRemoveCallback(ref data, out callback, out callbackInfo))
			{
				callback(ref callbackInfo);
			}
		}

		[MonoPInvokeCallback(typeof(OnReadFileCompleteCallbackInternal))]
		internal static void OnReadFileCompleteCallbackInternalImplementation(ref ReadFileCallbackInfoInternal data)
		{
			OnReadFileCompleteCallback callback;
			ReadFileCallbackInfo callbackInfo;
			if (Helper.TryGetAndRemoveCallback(ref data, out callback, out callbackInfo))
			{
				callback(ref callbackInfo);
			}
		}

		[MonoPInvokeCallback(typeof(OnReadFileDataCallbackInternal))]
		internal static ReadResult OnReadFileDataCallbackInternalImplementation(ref ReadFileDataCallbackInfoInternal data)
		{
			OnReadFileDataCallback callback;
			ReadFileDataCallbackInfo callbackInfo;
			if (Helper.TryGetStructCallback(ref data, out callback, out callbackInfo))
			{
				ReadFileDataCallbackInfo dataObj;
				Helper.Get(ref data, out dataObj);

				var funcResult = callback(ref dataObj);

				return funcResult;
			}

			return Helper.GetDefault<ReadResult>();
		}
	}
}