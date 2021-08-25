﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Stylet;

namespace Anidow.Validators
{
    public class FluentValidationAdapter<T> : IModelValidator<T>
    {
        private readonly IValidator<T> _validator;
        private T _subject;

        public FluentValidationAdapter(IValidator<T> validator)
        {
            _validator = validator;
        }

        public void Initialize(object subject)
        {
            _subject = (T) subject;
        }

        public async Task<IEnumerable<string>> ValidatePropertyAsync(string propertyName)
        {
            // If someone's calling us synchronously, and ValidationAsync does not complete synchronously,
            // we'll deadlock unless we continue on another thread.
            try
            {
                var errors = await _validator.ValidateAsync(_subject, CancellationToken.None).ConfigureAwait(false);
                return errors.Errors.Select(x => x.ErrorMessage);
            }
            catch (Exception)
            {
                // ignore
                return default;
            }
        }

        public async Task<Dictionary<string, IEnumerable<string>>> ValidateAllPropertiesAsync()
        {
            // If someone's calling us synchronously, and ValidationAsync does not complete synchronously,
            // we'll deadlock unless we continue on another thread.
            return (await _validator.ValidateAsync(_subject).ConfigureAwait(false))
                   .Errors.GroupBy(x => x.PropertyName)
                   .ToDictionary(x => x.Key, x => x.Select(failure => failure.ErrorMessage));
        }
    }
}