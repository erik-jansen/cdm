// <copyright file="StorageModel.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace CdmManifestSaveToAdls
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// StorageModel abstract class
    /// </summary>
    public abstract class StorageModel
    {
        /// <summary>
        /// Holds the StorageType value for StorageModel
        /// </summary>
        public string StorageType { get; set; }

        /// <summary>
        /// Holds the NameSpace value for StorageModel
        /// </summary>
        public string NameSpace { get; set; }

        /// <summary>
        /// Holds the RootManifestLocation value for StorageModel
        /// </summary>
        public string RootManifestLocation { get; set; }

        /// <summary>
        /// Holds the RootManifestName value for StorageModel
        /// </summary>
        public string RootManifestName { get; set; }

        /// <summary>
        /// Custom core files. example: IDMConcepts.cdm.json.
        /// </summary>
        public List<string> CustomCoreFiles { get; set; }

        /// <summary>
        /// Returns true if StorageType equals StorageTypeValues.LOCAL
        /// </summary>
        public bool IsLocalStorageModel()
        {
            return this.StorageType.Equals("LOCAL", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns true if StorageType equals StorageTypeValues.ADLS
        /// </summary>
        public bool IsADLSStorageModel()
        {
            return this.StorageType.Equals("ADLS", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns true if StorageType equals StorageTypeValues.NoOp
        /// </summary>
        public bool IsValidationStorageModel()
        {
            return this.StorageType.Equals("VALIDATION", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns true if StorageType equals StorageTypeValues.ZIP
        /// </summary>
        public bool IsZipStorageModel()
        {
            return this.StorageType.Equals("ZIP", StringComparison.OrdinalIgnoreCase);
        }

        public List<string> GetCustomCoreFileList()
        {
            if (this.CustomCoreFiles == null)
            {
                return new List<string>();
            }

            return this.CustomCoreFiles;
        }
    }
}