﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using ImageMaker.CommonViewModels.ViewModels;
using ImageMaker.Data.Repositories;

namespace ImageMaker.CommonViewModels.Providers
{
    public class PatternViewModelProvider
    {
        private readonly IImageRepository _imageDataProvider;
        private readonly IMappingEngine _mappingEngine;

        public PatternViewModelProvider(IImageRepository imageDataProvider, IMappingEngine mappingEngine)
        {
            _imageDataProvider = imageDataProvider;
            _mappingEngine = mappingEngine;
        }

        public IEnumerable<TemplateViewModel> GetPatterns()
        {
            var result =  _imageDataProvider.GetTemplates();
            return result.Select(_mappingEngine.Map<TemplateViewModel>);
            //return _imageDataProvider.GetCompositions().Select(_mappingEngine.Map<TemplateViewModel>);
        }

        public async Task<IEnumerable<TemplateViewModel>> GetPatternsAsync()
        {
            var result = await _imageDataProvider.GetTemplatesAsync();
            return result.Select(_mappingEngine.Map<TemplateViewModel>);
        } 

        //public IEnumerable<CompositionViewModel> GetPatterns()
        //{
        //    return _imageDataProvider.GetCompositions().Select(_mappingEngine.Map<CompositionViewModel>);
        //}

        //public async Task<IEnumerable<CompositionViewModel>> GetPatternsAsync()
        //{
        //    var result = await _imageDataProvider.GetCompositionsAsync();
        //    return result.Select(_mappingEngine.Map<CompositionViewModel>);
        //} 
    }
}
