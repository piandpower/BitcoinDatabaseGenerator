﻿//-----------------------------------------------------------------------
// <copyright file="BitcoinDataLayerValidation.cs">
// Copyright © Ladislau Molnar. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace BitcoinDataLayerAdoNet
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using AdoNetHelpers;
    using BitcoinDataLayerAdoNet.DataSets;

    /// <summary>
    /// This code section contains the methods of class <see cref="BitcoinDataLayer" /> that retrieve validation datasets.
    /// </summary>
    public partial class BitcoinDataLayer : IDisposable
    {
        public ValidationDataSetInfo<ValidationBlockchainDataSet> GetValidationBlockchainDataSet(int maxBlockFileId)
        {
            const string sqlCommandText = @"
                SELECT 
                    COUNT(1) AS BlockCount,
                    SUM(TransactionCount) AS TransactionCount,
                    SUM(TransactionInputCount) AS TransactionInputCount,
                    SUM(TotalInputBtc) AS TotalInputBtc,
                    SUM(TransactionOutputCount) AS TransactionOutputCount,
                    SUM(TotalOutputBtc) AS TotalOutputBtc,
                    SUM(TransactionFeeBtc) AS TransactionFeeBtc,
                    SUM(TotalUnspentOutputBtc) AS TotalUnspentOutputBtc
                FROM View_BlockAggregated
                WHERE BlockFileId <= @MaxBlockFileId";

            return this.GetValidationDataSetInfo<ValidationBlockchainDataSet>(
                sqlCommandText,
                AdoNetLayer.CreateInputParameter("@MaxBlockFileId", SqlDbType.Int, maxBlockFileId));
        }

        public ValidationDataSetInfo<ValidationBlockFilesDataSet> GetValidationBlockFilesDataSet(int maxBlockFileId)
        {
            const string sqlCommandText = @"
                SELECT 
                    BlockFile.BlockFileId,
                    BlockFile.FileName,
                    T1.BlockCount,
                    T1.TransactionCount,
                    T1.TransactionInputCount,
                    T1.TotalInputBtc,
                    T1.TransactionOutputCount,
                    T1.TotalOutputBtc,
                    T1.TransactionFeeBtc,
                    T1.TotalUnspentOutputBtc
                FROM BlockFile
                INNER JOIN (
                    SELECT 
                        BlockFileId,
                        COUNT(1) AS BlockCount,
                        SUM(TransactionCount) AS TransactionCount,
                        SUM(TransactionInputCount) AS TransactionInputCount,
                        SUM(TotalInputBtc) AS TotalInputBtc,
                        SUM(TransactionOutputCount) AS TransactionOutputCount,
                        SUM(TotalOutputBtc) AS TotalOutputBtc,
                        SUM(TransactionFeeBtc) AS TransactionFeeBtc,
                        SUM(TotalUnspentOutputBtc) AS TotalUnspentOutputBtc
                    FROM View_BlockAggregated
                    GROUP BY BlockFileId
                    ) AS T1
                    ON T1.BlockFileId = BlockFile.BlockFileId
                WHERE BlockFile.BlockFileId <= @MaxBlockFileId
                ORDER BY BlockFile.BlockFileId";

            return this.GetValidationDataSetInfo<ValidationBlockFilesDataSet>(
                sqlCommandText,
                AdoNetLayer.CreateInputParameter("@MaxBlockFileId", SqlDbType.Int, maxBlockFileId));
        }

        public ValidationDataSetInfo<ValidationBlockDataSet> GetValidationBlockSampleDataSet(long maxBlockFileId, int sampleRatio)
        {
            const string sqlCommandText = @"
                SELECT 
                    BlockId,
                    BlockFileId,
                    BlockVersion,
                    BlockHash,
                    PreviousBlockHash,
                    BlockTimestamp,
                    TransactionCount,
                    TransactionInputCount,
                    TotalInputBtc,
                    TransactionOutputCount,
                    TotalOutputBtc,
                    TransactionFeeBtc,
                    TotalUnspentOutputBtc
                FROM View_BlockAggregated
                WHERE 
                    BlockId <= (SELECT MAX(BlockId) FROM Block WHERE BlockFileId <= @MaxBlockFileId) 
                    AND BlockId % @SampleRatio = 0
                ORDER BY BlockId";

            return this.GetValidationDataSetInfo<ValidationBlockDataSet>(
                sqlCommandText,
                AdoNetLayer.CreateInputParameter("@MaxBlockFileId", SqlDbType.BigInt, maxBlockFileId),
                AdoNetLayer.CreateInputParameter("@SampleRatio", SqlDbType.Int, sampleRatio));
        }

        public ValidationDataSetInfo<ValidationTransactionDataSet> GetValidationTransactionSampleDataSet(int maxBlockFileId, int sampleRatio)
        {
            const string sqlCommandText = @"
                SELECT 
                    BitcoinTransactionId,
                    BlockId,
                    TransactionHash,
                    TransactionVersion,
                    TransactionLockTime,
                    TransactionInputCount,
                    TotalInputBtc,
                    TransactionOutputCount,
                    TotalOutputBtc,
                    TransactionFeeBtc,
                    TotalUnspentOutputBtc
                FROM View_TransactionAggregated 
                WHERE 
                    BitcoinTransactionId <= (
                        SELECT MAX(BitcoinTransactionId) 
                        FROM BitcoinTransaction 
                        INNER JOIN Block ON Block.BlockId = BitcoinTransaction.BlockId
                        WHERE Block.BlockFileId <= @MaxBlockFileId) 
                    AND BitcoinTransactionId % @SampleRatio = 0
                ORDER BY BitcoinTransactionId";

            return this.GetValidationDataSetInfo<ValidationTransactionDataSet>(
                sqlCommandText,
                AdoNetLayer.CreateInputParameter("@MaxBlockFileId", SqlDbType.BigInt, maxBlockFileId),
                AdoNetLayer.CreateInputParameter("@SampleRatio", SqlDbType.Int, sampleRatio));
        }

        public ValidationDataSetInfo<ValidationTransactionInputDataSet> GetValidationTransactionInputSampleDataSet(int maxBlockFileId, int sampleRatio)
        {
            const string sqlCommandText = @"
                SELECT 
                    TransactionInput.TransactionInputId,
                    TransactionInput.BitcoinTransactionId,
                    TransactionInput.SourceTransactionOutputId,
                    (   SELECT SUM(TransactionOutput.OutputValueBtc)
                        FROM TransactionOutput
                        WHERE TransactionOutput.TransactionOutputId = TransactionInput.SourceTransactionOutputId
                    ) AS TransactionInputValueBtc,
                    TransactionInputSource.SourceTransactionHash,
                    TransactionInputSource.SourceTransactionOutputIndex
                FROM TransactionInput
                INNER JOIN TransactionInputSource ON TransactionInputSource.TransactionInputId = TransactionInput.TransactionInputId
                WHERE 
                    TransactionInput.TransactionInputId <= (
                        SELECT MAX(TransactionInputId) 
                        FROM TransactionInput
                        INNER JOIN BitcoinTransaction ON BitcoinTransaction.BitcoinTransactionId = TransactionInput.BitcoinTransactionId
                        INNER JOIN Block ON Block.BlockId = BitcoinTransaction.BlockId
                        WHERE Block.BlockFileId <= @MaxBlockFileId) 
                    AND TransactionInput.TransactionInputId % @SampleRatio = 0
                ORDER BY TransactionInput.TransactionInputId";

            return this.GetValidationDataSetInfo<ValidationTransactionInputDataSet>(
                sqlCommandText,
                AdoNetLayer.CreateInputParameter("@MaxBlockFileId", SqlDbType.BigInt, maxBlockFileId),
                AdoNetLayer.CreateInputParameter("@SampleRatio", SqlDbType.Int, sampleRatio));
        }

        public ValidationDataSetInfo<ValidationTransactionOutputDataSet> GetValidationTransactionOutputSampleDataSet(int maxBlockFileId, int sampleRatio)
        {
            const string sqlCommandText = @"
                SELECT 
                    TransactionOutput.TransactionOutputId,
                    TransactionOutput.BitcoinTransactionId,
                    TransactionOutput.OutputIndex,
                    TransactionOutput.OutputValueBtc,
                    TransactionOutput.OutputScript,
                    CASE 
                        WHEN EXISTS (SELECT * FROM TransactionInput WHERE SourceTransactionOutputId = TransactionOutput.OutputIndex)
                        THEN 1
                        ELSE 0
                        END
                    AS IsSpent
                FROM TransactionOutput
                WHERE 
                    TransactionOutput.TransactionOutputId <= (
                        SELECT MAX(TransactionOutputId) 
                        FROM TransactionOutput
                        INNER JOIN BitcoinTransaction ON BitcoinTransaction.BitcoinTransactionId = TransactionOutput.BitcoinTransactionId
                        INNER JOIN Block ON Block.BlockId = BitcoinTransaction.BlockId
                        WHERE Block.BlockFileId <= @MaxBlockFileId) 
                    AND TransactionOutput.TransactionOutputId % @SampleRatio = 0
                ORDER BY TransactionOutput.TransactionOutputId";

            return this.GetValidationDataSetInfo<ValidationTransactionOutputDataSet>(
                sqlCommandText,
                AdoNetLayer.CreateInputParameter("@MaxBlockFileId", SqlDbType.BigInt, maxBlockFileId),
                AdoNetLayer.CreateInputParameter("@SampleRatio", SqlDbType.Int, sampleRatio));
        }

        private ValidationDataSetInfo<T> GetValidationDataSetInfo<T>(string sqlCommandText, params SqlParameter[] sqlParameters) where T : DataSet, new()
        {
            T dataset = this.GetDataSet<T>(sqlCommandText, sqlParameters);
            return new ValidationDataSetInfo<T>(dataset, sqlCommandText, sqlParameters);
        }
    }
}
