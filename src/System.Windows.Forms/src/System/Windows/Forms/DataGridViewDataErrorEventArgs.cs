﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows.Forms;

public class DataGridViewDataErrorEventArgs : DataGridViewCellCancelEventArgs
{
    private bool _throwException;

    public DataGridViewDataErrorEventArgs(Exception? exception, int columnIndex, int rowIndex, DataGridViewDataErrorContexts context)
        : base(columnIndex, rowIndex)
    {
        Exception = exception;
        Context = context;
    }

    public DataGridViewDataErrorContexts Context { get; }

    public Exception? Exception { get; }

    public bool ThrowException
    {
        get => _throwException;
        set
        {
            if (value && Exception is null)
            {
                throw new ArgumentException(SR.DataGridView_CannotThrowNullException);
            }

            _throwException = value;
        }
    }
}
