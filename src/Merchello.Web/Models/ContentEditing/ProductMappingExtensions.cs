﻿using System.Linq;
using System.Text.RegularExpressions;
using Merchello.Core;
using Merchello.Core.Models;
using System;
using System.Collections.Generic;

namespace Merchello.Web.Models.ContentEditing
{
    using System.Security.Cryptography;

    using umbraco.presentation.actions;

    internal static class ProductMappingExtensions
    {

        #region IProduct

        internal static IProduct ToProduct(this ProductDisplay productDisplay, IProduct destination)
        {
            if (productDisplay.Key != Guid.Empty)
            {
                destination.Key = productDisplay.Key;
            }

            productDisplay.CatalogInventories = productDisplay.CatalogInventories ?? Enumerable.Empty<CatalogInventoryDisplay>();
            productDisplay.ProductOptions = productDisplay.ProductOptions ?? Enumerable.Empty<ProductOptionDisplay>();

            destination.Name = productDisplay.Name;
            destination.Sku = productDisplay.Sku;
            destination.Price = productDisplay.Price;
            destination.CostOfGoods = productDisplay.CostOfGoods;
            destination.SalePrice = productDisplay.SalePrice;
            destination.OnSale = productDisplay.OnSale;
            destination.Manufacturer = productDisplay.Manufacturer;
            destination.ManufacturerModelNumber = productDisplay.ManufacturerModelNumber;
            destination.Weight = productDisplay.Weight;
            destination.Length = productDisplay.Length;
            destination.Width = productDisplay.Width;
            destination.Height = productDisplay.Height;
            destination.Barcode = productDisplay.Barcode;
            destination.Available = productDisplay.Available;
            destination.TrackInventory = productDisplay.TrackInventory;
            destination.OutOfStockPurchase = productDisplay.OutOfStockPurchase;
            destination.Taxable = productDisplay.Taxable;
            destination.Shippable = productDisplay.Shippable;
            destination.Download = productDisplay.Download;
            destination.DownloadMediaId = productDisplay.DownloadMediaId;

            // We need to refactor the CatalogInventories to not be immutable if we are
            // going to need to do operations like this.  In the UI, the user "unchecks" a catalog that was
            // previously checked - so we need to remove it.
            var deletedCatalogKeys =
                destination.CatalogInventories.Where(
                    x => !productDisplay.CatalogInventories.Select(ci => ci.CatalogKey).Contains(x.CatalogKey)).Select(x => x.CatalogKey).ToArray();
            foreach (var deletedCatalogKey in deletedCatalogKeys)
            {
                ((Product)destination).MasterVariant.RemoveFromCatalogInventory(deletedCatalogKey);
            }

            foreach (var catalogInventory in productDisplay.CatalogInventories)
            {
                var catInv = destination.CatalogInventories.FirstOrDefault(x => x.CatalogKey == catalogInventory.CatalogKey);

                if (catInv != null)
                {
                    var destinationCatalogInventory = catInv;

                    destinationCatalogInventory = catalogInventory.ToCatalogInventory(destinationCatalogInventory);
                }
                else
                {
                    //// Add to a new catalog
                    ((Product)destination).MasterVariant.AddToCatalogInventory(new CatalogInventory(catalogInventory.CatalogKey, catalogInventory.ProductVariantKey)
                                                                                   {
                                                                                       Location = catalogInventory.Location,
                                                                                       Count = catalogInventory.Count,
                                                                                       LowCount = catalogInventory.LowCount
                                                                                   });
                }
            }

            // Fix option deletion here #M-161
            // remove any product options that exist in destination and do not exist in productDisplay
            var removers = destination.ProductOptions.Where(x => !productDisplay.ProductOptions.Select(pd => pd.Key).Contains(x.Key)).Select(x => x.Key).ToList();
            foreach (var remove in removers)
            {
                destination.ProductOptions.RemoveItem(remove);
            }


            foreach (var option in productDisplay.ProductOptions)
            {
                IProductOption destinationProductOption;
                

                if (destination.ProductOptions.Contains(option.Key))
                {
                    destinationProductOption = destination.ProductOptions[option.Key];

                    destinationProductOption = option.ToProductOption(destinationProductOption);
                }
                else
                {
                    destinationProductOption = new ProductOption(option.Name, option.Required);

                    destinationProductOption = option.ToProductOption(destinationProductOption);
                }

                destination.ProductOptions.Add(destinationProductOption);
            }

            //var existingVariants = destination.ProductVariants.ToArray();
            //destination.ProductVariants.Clear();
            //foreach (var variant in productDisplay.ProductVariants.Where(variant => existingVariants.Any(x => x.Key == variant.Key)))
            //{
            //    destination.ProductVariants.Add(variant.ToProductVariant(existingVariants.FirstOrDefault(x => x.Key == variant.Key)));
            //}
            
            return destination;
        }

        internal static IProduct ToProduct(this ProductDisplay productDisplay, string name, string sku, decimal price)
        {
            var destination = MerchelloContext.Current.Services.ProductService.CreateProduct(name, sku, price);
            return ToProduct(productDisplay, destination);
        }

        #endregion

        #region ProductDisplay

        internal static ProductDisplay ToProductDisplay(this IProduct product)
        {            
            return AutoMapper.Mapper.Map<ProductDisplay>(product);
        }
               
        #endregion


        #region IProductAttribute


        internal static IProductAttribute ToProductAttribute(this ProductAttributeDisplay productAttributeDisplay, IProductAttribute destinationProductAttribute)
        {
            if (productAttributeDisplay.Key != Guid.Empty)
            {
                destinationProductAttribute.Key = productAttributeDisplay.Key;
            }
            destinationProductAttribute.Name = productAttributeDisplay.Name;
            destinationProductAttribute.Sku = productAttributeDisplay.Sku;
            destinationProductAttribute.OptionKey = productAttributeDisplay.OptionKey;
            destinationProductAttribute.SortOrder = productAttributeDisplay.SortOrder;

            return destinationProductAttribute;
        }

        internal static ProductAttributeDisplay ToProductAttributeDisplay(this IProductAttribute productAttribute)
        {            
            return AutoMapper.Mapper.Map<ProductAttributeDisplay>(productAttribute);
        }


        #endregion

        #region IProductOption

        internal static IProductOption ToProductOption(this ProductOptionDisplay productOptionDisplay, IProductOption destinationProductOption)
        {
            if (productOptionDisplay.Key != Guid.Empty)
            {
                destinationProductOption.Key = productOptionDisplay.Key;
            }
            destinationProductOption.Required = productOptionDisplay.Required;
            destinationProductOption.SortOrder = productOptionDisplay.SortOrder;


            // Fix with option deletion here #M-161 #M-150
            // remove any product choices that exist in destination and do not exist in productDisplay
            var removers = destinationProductOption.Choices.Where(x => !productOptionDisplay.Choices.Select(pd => pd.Key).Contains(x.Key)).Select(x => x.Key).ToArray();
            foreach (var remove in removers)
            {
                destinationProductOption.Choices.RemoveItem(remove);
            }

            foreach (var choice in productOptionDisplay.Choices)
            {
                // Sets the sku if it is empty - fixes M-170
                // http://issues.merchello.com/youtrack/issue/M-170
                if (string.IsNullOrEmpty(choice.Sku))
                {
                    choice.Sku = Regex.Replace(choice.Name, "[^0-9a-zA-Z]+", "");
                }

                IProductAttribute destinationProductAttribute;

                
                if (destinationProductOption.Choices.Contains(choice.Sku))
                {
                    destinationProductAttribute = destinationProductOption.Choices[choice.Key];

                    destinationProductAttribute = choice.ToProductAttribute(destinationProductAttribute);
                }
                else
                {
                    destinationProductAttribute = new ProductAttribute(choice.Name, choice.Sku);

                    destinationProductAttribute = choice.ToProductAttribute(destinationProductAttribute);
                }

                destinationProductOption.Choices.Add(destinationProductAttribute);
            }

            return destinationProductOption;
        }

        internal static ProductOptionDisplay ToProductOptionDisplay(this IProductOption productOption)
        {            
            return AutoMapper.Mapper.Map<ProductOptionDisplay>(productOption);
        }

        #endregion

        #region IProductVariant

        internal static ProductVariantDisplay ToProductVariantDisplay(this IProductVariant productVariant)
        {            
            return AutoMapper.Mapper.Map<ProductVariantDisplay>(productVariant);
        }

        internal static IProductVariant ToProductVariant(this ProductVariantDisplay productVariantDisplay, IProductVariant destination)
        {
            if (productVariantDisplay.Key != Guid.Empty)
            {
                destination.Key = productVariantDisplay.Key;
            }
            if( !String.IsNullOrEmpty(productVariantDisplay.Name) )
            {
                destination.Name = productVariantDisplay.Name;
            }
            if( !String.IsNullOrEmpty(productVariantDisplay.Sku) )
            {
                destination.Sku = productVariantDisplay.Sku;
            }
            destination.Price = productVariantDisplay.Price;
            destination.CostOfGoods = productVariantDisplay.CostOfGoods;
            destination.SalePrice = productVariantDisplay.SalePrice;
            destination.OnSale = productVariantDisplay.OnSale;
            destination.Manufacturer = productVariantDisplay.Manufacturer;
            destination.ManufacturerModelNumber = productVariantDisplay.ManufacturerModelNumber;
            destination.Weight = productVariantDisplay.Weight;
            destination.Length = productVariantDisplay.Length;
            destination.Width = productVariantDisplay.Width;
            destination.Height = productVariantDisplay.Height;
            destination.Barcode = productVariantDisplay.Barcode;
            destination.Available = productVariantDisplay.Available;
            destination.TrackInventory = productVariantDisplay.TrackInventory;
            destination.OutOfStockPurchase = productVariantDisplay.OutOfStockPurchase;
            destination.Taxable = productVariantDisplay.Taxable;
            destination.Shippable = productVariantDisplay.Shippable;
            destination.Download = productVariantDisplay.Download;
            destination.DownloadMediaId = productVariantDisplay.DownloadMediaId;

            destination.ProductKey = productVariantDisplay.ProductKey;

            // We need to refactor the CatalogInventories to not be immutable if we are
            // going to need to do operations like this.  In the UI, the user "unchecks" a catalog that was
            // previously checked - so we need to remove it.
            var deletedCatalogKeys =
                destination.CatalogInventories.Where(
                    x => !productVariantDisplay.CatalogInventories.Select(ci => ci.CatalogKey).Contains(x.CatalogKey)).Select(x => x.CatalogKey).ToArray();
            foreach (var deletedCatalogKey in deletedCatalogKeys)
            {
                destination.RemoveFromCatalogInventory(deletedCatalogKey);
            }

            foreach (var catalogInventory in productVariantDisplay.CatalogInventories)
            {
				var catInv = destination.CatalogInventories.FirstOrDefault(x => x.CatalogKey == catalogInventory.CatalogKey);
				
				if (catInv != null)
				{
					var destinationCatalogInventory = catInv;
				
					destinationCatalogInventory = catalogInventory.ToCatalogInventory(destinationCatalogInventory);
				}
				else if (!Guid.Empty.Equals(catalogInventory.CatalogKey) && destination.HasIdentity)
				{
					//// Add to a new catalog
					destination.AddToCatalogInventory(catalogInventory.CatalogKey);
				}
            }

            foreach (var attribute in productVariantDisplay.Attributes)
            {
                IProductAttribute destinationProductAttribute;

                var attr = destination.Attributes.FirstOrDefault(x => x.Key == attribute.Key);
                if (attr != null)
                {
                    destinationProductAttribute = attr;

                    destinationProductAttribute = attribute.ToProductAttribute(destinationProductAttribute);
                }
                else
                {
                    destinationProductAttribute = new ProductAttribute(attribute.Name, attribute.Sku);

                    destinationProductAttribute = attribute.ToProductAttribute(destinationProductAttribute);

                    ProductAttributeCollection variantAttributes = destination.Attributes as ProductAttributeCollection;
                    variantAttributes.Add(destinationProductAttribute);
                }
            }

            return destination;
        }

        #endregion
    }
}
