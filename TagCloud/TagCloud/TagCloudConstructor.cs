﻿using TagsCloudLayouter;
using Result;

namespace TagCloud;

public class TagCloudConstructor
{
    private readonly ApplicationProperties applicationProperties;
    private readonly ICloudDrawer drawer;
    private readonly FrequencyDictionary frequencyDictionary;
    private readonly ICloudLayouter layouter;
    private readonly IWordsParser parser;
    private readonly SizeByFrequency sizeByFrequency;
    private readonly IFileLoader textLoader;
    private readonly IWordPreprocessor wordPreprocessor;

    public TagCloudConstructor(
        ICloudDrawer drawer,
        IFileLoader textLoader,
        ApplicationProperties applicationProperties,
        IWordsParser parser,
        SizeByFrequency sizeByFrequency,
        ICloudLayouter layouter,
        IWordPreprocessor wordPreprocessor,
        FrequencyDictionary frequencyDictionary)
    {
        this.drawer = drawer;
        this.textLoader = textLoader;
        this.applicationProperties = applicationProperties;
        this.parser = parser;
        this.sizeByFrequency = sizeByFrequency;
        this.layouter = layouter;
        this.wordPreprocessor = wordPreprocessor;
        this.frequencyDictionary = frequencyDictionary;
    }

    public Result<Bitmap> Construct()
    {
        var text = textLoader.Load(applicationProperties.Path);
        if (!text.IsSuccess)
            return new Result<Bitmap>(null, $"TextLoader: {text.Error}");
        
        var words = parser.Parse(text.Value);
        if (!words.IsSuccess && words.Value is not null)
            return new Result<Bitmap>(null, $"Parser: {words.Error}");
        
        var processedWords = wordPreprocessor.Process(words, applicationProperties.CloudProperties.ExcludedWords);
        var wordsFrequency = frequencyDictionary.GetWordsFrequency(processedWords);
        var texts = sizeByFrequency.ResizeAll(wordsFrequency).ToList();
        layouter.PlaceTexts(texts);
        
        return AreAllWordsWithinImageBounds(applicationProperties.SizeProperties, applicationProperties.CloudProperties) 
            ? new Result<Bitmap>(drawer.Draw(texts)) 
            : new Result<Bitmap>(null, "Words are not in bound of image");
    }

    private bool AreAllWordsWithinImageBounds(SizeProperties sizeProperties, CloudProperties cloudProperties)
    {
        var imagePosition = Point.Subtract(
            cloudProperties.Center,
            sizeProperties.ImageSize / 2);
        var imageZone = new Rectangle(imagePosition, sizeProperties.ImageSize);
        return layouter.Rectangles.All(r => imageZone.Contains(r));
    }
}