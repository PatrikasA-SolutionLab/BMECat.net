/*
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 * 
 *   http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 */
 using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace BMECat.net
{
    public class BMECatWriter
    {
        public ProductCatalog Catalog { get; private set; }
        private XmlTextWriter Writer { get; set; }


        public async Task SaveAsync(ProductCatalog catalog, Stream stream, BMECatExtensions extensions = null, BMECatVersion version = BMECatVersion.Version2005)
        {
            if (!stream.CanWrite || !stream.CanSeek)
            {
                throw new IllegalStreamException("Cannot write to stream");
            }

            long streamPosition = stream.Position;

            this.Catalog = catalog;
            this.Writer = new XmlTextWriter(stream, Encoding.UTF8);
            Writer.Formatting = Formatting.Indented;
            Writer.WriteStartDocument();

            #region XML-Kopfbereich
            Writer.WriteStartElement("BMECAT");
            Writer.WriteAttributeString("version", _getVersionString(version));
            Writer.WriteAttributeString("xmlns", _getNamespace(version));
            #endregion // !XML-Kopfbereich

            #region Header
            Writer.WriteStartElement("HEADER");
            _writeOptionalElementString(Writer, "GENERATOR_INFO", this.Catalog.GeneratorInfo);

            Writer.WriteStartElement("CATALOG");
            foreach (LanguageCodes language in this.Catalog.Languages)
            {
                Writer.WriteElementString("LANGUAGE", language.EnumToString());
            }
            Writer.WriteElementString("CATALOG_ID", this.Catalog.CatalogId); // Pflichtfeld
            Writer.WriteElementString("CATALOG_VERSION", this.Catalog.CatalogVersion); // Pflichtfeld
            _writeOptionalElementString(Writer, "CATALOG_NAME", this.Catalog.CatalogName);
            _writeDateTime(elementName: "GENERATION_DATE", date: this.Catalog.GenerationDate);
            Writer.WriteElementString("CURRENCY", this.Catalog.Currency.EnumToString());
            _writeTransport(Writer, this.Catalog.Transport);
            Writer.WriteEndElement(); // !CATALOG

            _writeParty("BUYER", this.Catalog.Buyer);
            _writeAgreement();
            _writeParty("SUPPLIER", this.Catalog.Supplier);

            Writer.WriteEndElement(); // !HEADER
            #endregion // !Header

            #region PRODUCTS
            Writer.WriteStartElement("T_NEW_CATALOG");
            _writeCatalogStructures();
            foreach(Product product in this.Catalog.Products)
            {
                Writer.WriteStartElement("PRODUCT");
                Writer.WriteAttributeString("mode", "new");
                _writeOptionalElementString(Writer, "SUPPLIER_PID", product.No);

                Writer.WriteStartElement("PRODUCT_DETAILS");
                _writeOptionalElementString(Writer, "DESCRIPTION_SHORT", product.DescriptionShort);
                _writeOptionalElementString(Writer, "DESCRIPTION_LONG", product.DescriptionLong);

                foreach(ProductId id in product.PIds)
                {
                    _writeOptionalElementString(Writer, "INTERNATIONAL_PID", id.Id, new Dictionary<string, string>() { { "type", id.Type.EnumToString() } });
                }

                _writeOptionalElementString(Writer, "MANUFACTURER_PID", String.Format("{0}", product.ManufacturerPID));
                _writeOptionalElementString(Writer, "MANUFACTURER_NAME", String.Format("{0}", product.ManufacturerName));
                _writeOptionalElementString(Writer, "MANUFACTURER_TYPE_DESCR", String.Format("{0}", product.ManufacturerTypeDescription));
                _writeOptionalElementString(Writer, "ERP_GROUP_SUPPLIER", String.Format("{0}", product.ERPGroupSupplier));
                _writeOptionalElementString(Writer, "ERP_GROUP_BUYER", String.Format("{0}", product.ERPGroupBuyer));

                if (product.Keywords != null)
                {
                    foreach (string keyword in product.Keywords)
                    {
                        _writeOptionalElementString(Writer, "KEYWORD", keyword);
                    }
                }

                Writer.WriteEndElement(); // !PRODUCT_DETAILS

                _writeFeatureSets(product.FeatureSets, extensions);

                if (product.OrderDetails != null)
                {
                    Writer.WriteStartElement("PRODUCT_ORDER_DETAILS");
                    _writeOptionalElementString(Writer, "ORDER_UNIT", product.OrderDetails.OrderUnit);
                    _writeOptionalElementString(Writer, "CONTENT_UNIT", product.OrderDetails.ContentUnit);
                    Writer.WriteEndElement(); // !PRODUCT_ORDER_DETAILS
                }

                if ((product.Prices != null) && (product.Prices.Count > 0))
                {
                    Writer.WriteStartElement("PRODUCT_PRICE_DETAILS");

                    foreach (ProductPrice price in product.Prices)
                    {
                        string priceTypeStr = price.PriceType.EnumToString();
                        if (string.IsNullOrEmpty(priceTypeStr))
                        {
                            continue;
                        }
                        Writer.WriteStartElement("PRODUCT_PRICE");
                        Writer.WriteAttributeString("price_type", priceTypeStr);
                        Writer.WriteElementString("PRICE_AMOUNT", price.Amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
                        Writer.WriteElementString("PRICE_CURRENCY", price.Currency.ToString());
                        Writer.WriteElementString("TAX", price.Tax.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
                        if (price.LowerBound.HasValue)
                        {
                            Writer.WriteElementString("LOWER_BOUND", price.LowerBound.Value.ToString());
                        }
                        Writer.WriteEndElement(); // !PRODUCT_PRICE
                    }
                    Writer.WriteEndElement(); // !PRODUCT_PRICE_DETAILS
                }

                _writeMimeInfos(product.MimeInfos);
                _writeLogisticsDetails(product.LogisticsDetails);
                _writeReferences(product.References);
                _writeCatalogGroupMappings(product.ProductCatalogGroupMappings);

                Writer.WriteEndElement(); // !PRODUCT
            }
            Writer.WriteEndElement(); // !T_NEW_CATALOG
            #endregion // !ARTICLES


            Writer.WriteEndElement(); // !BMECAT
            Writer.WriteEndDocument();
            Writer.Flush();

            stream.Seek(streamPosition, SeekOrigin.Begin);

            await Task.CompletedTask;
        } // !SaveAsync()


        public async Task SaveAsync(ProductCatalog catalog, string filename, BMECatExtensions extensions = null, BMECatVersion version = BMECatVersion.Version2005)
        {
            FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write);
            await SaveAsync(catalog, fs, extensions, version);
            fs.Flush();
            fs.Close();
        } // !SaveAsync()


        private void _writeTransport(XmlTextWriter writer, TransportConditions transportCondition)
        {
            if (transportCondition == null)
            {
                return;
            }

            writer.WriteStartElement("TRANSPORT");
            writer.WriteElementString("INCOTERM", transportCondition.Incoterm.EnumToString());
            _writeOptionalElementString(Writer, "LOCATION", transportCondition.Location);
            _writeOptionalElementString(Writer, "TRANSPORT_REMARK", transportCondition.Remark);
            writer.WriteEndElement(); // !TRANSPORT
        } // !_writeTransport()


        private void _writeDateTime(string elementName, string typeAttribute = "", DateTime? date = null)
        {
            Writer.WriteStartElement(elementName);
            if (!string.IsNullOrEmpty(typeAttribute))
            {
                Writer.WriteAttributeString("type", typeAttribute);
            }
            /*
            Writer.WriteElementString("DATE", date.ToString("yyyy-dd-MM"));
            Writer.WriteElementString("TIME", date.ToString("hh:mm"));
            Writer.WriteElementString("TIMEZONE", date.ToString("zzz"));
            */
            if (date.HasValue)
            {
                Writer.WriteString(date.Value.ToString("yyyy-MM-ddTHH:mm:sszzz"));
            }
            Writer.WriteEndElement();
        } // !_writeDateTime()


        private string _formatDecimal(double value, int numDecimals = 2)
        {
            return _formatDecimal((decimal)value, numDecimals);
        } // !_formatDecimal()


        private string _formatDecimal(float value, int numDecimals = 2)
        {
            return _formatDecimal((decimal)value, numDecimals);
        } // !_formatDecimal()


        private string _formatDecimal(decimal value, int numDecimals = 2)
        {
            string formatString = "0.";
            for (int i = 0; i < numDecimals; i++)
            {
                formatString += "0";
            }

            return value.ToString(formatString).Replace(",", ".");
        } // !_formatDecimal()


        private void _writeOptionalElementString(XmlTextWriter writer, string tagName, string value, Dictionary<string, string> attributes = null)
        {
            if (!String.IsNullOrEmpty(value))
            {
                writer.WriteStartElement(tagName);
                if (attributes != null)
                {
                    foreach(KeyValuePair<string, string> attr in attributes)
                    {
                        writer.WriteAttributeString(attr.Key, attr.Value);
                    }
                }
                writer.WriteValue(value);
                writer.WriteEndElement();                
            }
        } // !_writeOptionalElementString


        private void _writeOptionalElementString(XmlTextWriter writer, string tagName, QuantityCode value, BMECatExtensions extensions = null)
        {
            if (value.ClearText != null)
            {
                writer.WriteElementString(tagName, value.ClearText);
            }
            else if (value.Code != QuantityCodes.Unknown)
            {
                if ((extensions != null) && (extensions.QuantityCodeConverter != null))
                {
                    writer.WriteElementString(tagName, extensions.QuantityCodeConverter.Convert(value.Code));
                }
                else
                {
                    writer.WriteElementString(tagName, value.Code.ToString());
                }
            }
        } // !_writeOptionalElementString()


        private static string _getNamespace(BMECatVersion version)
        {
            switch (version)
            {
                case BMECatVersion.Version12: return "http://www.bmecat.org/bmecat-1.2";
                case BMECatVersion.Version2005_1: return "http://www.bmecat.org/bmecat/2005.1";
                default: return "http://www.bmecat.org/bmecat/2005fd";
            }
        } // !_getNamespace()


        private static string _getVersionString(BMECatVersion version)
        {
            switch (version)
            {
                case BMECatVersion.Version12: return "1.2";
                case BMECatVersion.Version2005_1: return "2005.1";
                default: return "2005";
            }
        } // !_getVersionString()


        private void _writeParty(string role, Party party)
        {
            if (party == null)
            {
                return;
            }

            Writer.WriteStartElement(role);
            if (!string.IsNullOrEmpty(party.Id))
            {
                Writer.WriteStartElement($"{role}_ID");
                Writer.WriteAttributeString("type", $"{role.ToLower()}_specific");
                Writer.WriteString(party.Id);
                Writer.WriteEndElement();
            }
            _writeOptionalElementString(Writer, $"{role}_NAME", party.Name);

            Writer.WriteStartElement("ADDRESS");
            Writer.WriteAttributeString("type", role.ToLower());
            _writeOptionalElementString(Writer, "NAME", party.Name);
            _writeOptionalElementString(Writer, "NAME2", party.Name2);
            _writeOptionalElementString(Writer, "NAME3", party.Name3);
            _writeOptionalElementString(Writer, "DEPARTMENT", party.Department);
            _writeOptionalElementString(Writer, "CONTACT", party.ContactName);
            _writeOptionalElementString(Writer, "STREET", party.Street);
            _writeOptionalElementString(Writer, "ZIP", party.Zip);
            _writeOptionalElementString(Writer, "BOXNO", party.BoxNo);
            _writeOptionalElementString(Writer, "ZIPBOX", party.ZipBox);
            _writeOptionalElementString(Writer, "CITY", party.City);
            _writeOptionalElementString(Writer, "STATE", party.State);
            _writeOptionalElementString(Writer, "COUNTRY", party.Country);
            _writeOptionalElementString(Writer, "VAT_ID", party.VATID);
            _writeOptionalElementString(Writer, "PHONE", party.Phone);
            _writeOptionalElementString(Writer, "FAX", party.Fax);
            Writer.WriteEndElement(); // !ADDRESS

            Writer.WriteEndElement(); // !BUYER / SUPPLIER
        } // !_writeParty()


        private void _writeAgreement()
        {
            if (this.Catalog.Agreement == null)
            {
                return;
            }

            Writer.WriteStartElement("AGREEMENT");
            _writeOptionalElementString(Writer, "AGREEMENT_ID", this.Catalog.Agreement.Id);
            if (this.Catalog.Agreement.StartDate.HasValue)
            {
                _writeDateTime("DATETIME", "agreement_start_date", this.Catalog.Agreement.StartDate);
            }
            if (this.Catalog.Agreement.EndDate.HasValue)
            {
                _writeDateTime("DATETIME", "agreement_end_date", this.Catalog.Agreement.EndDate);
            }
            Writer.WriteEndElement(); // !AGREEMENT
        } // !_writeAgreement()


        private void _writeCatalogStructures()
        {
            if (this.Catalog.CatalogStructures == null || this.Catalog.CatalogStructures.Count == 0)
            {
                return;
            }

            Writer.WriteStartElement("CATALOG_GROUP_SYSTEM");
            foreach (CatalogStructure group in this.Catalog.CatalogStructures)
            {
                Writer.WriteStartElement("CATALOG_STRUCTURE");
                if (group.Type.HasValue)
                {
                    Writer.WriteAttributeString("type", group.Type.Value == CatalogStructureTypes.Leaf ? "leaf" : "node");
                }
                _writeOptionalElementString(Writer, "GROUP_ID", group.GroupId);
                _writeOptionalElementString(Writer, "GROUP_NAME", group.GroupName);
                _writeOptionalElementString(Writer, "PARENT_ID", group.ParentId);
                _writeOptionalElementString(Writer, "GROUP_ORDER", group.GroupOrder);
                _writeMimeInfos(group.MimeInfos);
                Writer.WriteEndElement(); // !CATALOG_STRUCTURE
            }
            Writer.WriteEndElement(); // !CATALOG_GROUP_SYSTEM
        } // !_writeCatalogStructures()


        private void _writeMimeInfos(IList<MimeInfo> mimeInfos)
        {
            if (mimeInfos == null || mimeInfos.Count == 0)
            {
                return;
            }

            Writer.WriteStartElement("MIME_INFO");
            foreach (MimeInfo mime in mimeInfos)
            {
                Writer.WriteStartElement("MIME");
                string mimeType = mime.MimeType != MimeTypes.Unknown ? mime.MimeType.EnumToString() : null;
                _writeOptionalElementString(Writer, "MIME_TYPE", mimeType);
                _writeOptionalElementString(Writer, "MIME_SOURCE", mime.Source);
                _writeOptionalElementString(Writer, "MIME_DESCR", mime.Description);
                _writeOptionalElementString(Writer, "MIME_ALT", mime.Alt);
                _writeOptionalElementString(Writer, "MIME_PURPOSE", mime.Purpose);
                if (mime.Order.HasValue)
                {
                    Writer.WriteElementString("MIME_ORDER", mime.Order.Value.ToString());
                }
                Writer.WriteEndElement(); // !MIME
            }
            Writer.WriteEndElement(); // !MIME_INFO
        } // !_writeMimeInfos()


        private void _writeFeatureSets(IList<FeatureSet> featureSets, BMECatExtensions extensions)
        {
            if (featureSets == null || featureSets.Count == 0)
            {
                return;
            }

            foreach (FeatureSet featureSet in featureSets)
            {
                Writer.WriteStartElement("PRODUCT_FEATURES");
                if (featureSet.FeatureClassificationSystem != null)
                {
                    _writeOptionalElementString(Writer, "REFERENCE_FEATURE_SYSTEM_NAME", featureSet.FeatureClassificationSystem.Classification);
                    foreach (FeatureClassificationSystemGroupId groupId in featureSet.FeatureClassificationSystem.GroupIds)
                    {
                        if (!string.IsNullOrEmpty(groupId.Name))
                        {
                            Writer.WriteStartElement("REFERENCE_FEATURE_GROUP_ID");
                            if (groupId.Type != FeatureClassificationSystemGroupIdTypes.Unknown)
                            {
                                Writer.WriteAttributeString("type", groupId.Type.ToString().ToLower());
                            }
                            Writer.WriteString(groupId.Name);
                            Writer.WriteEndElement();
                        }
                    }
                    _writeOptionalElementString(Writer, "REFERENCE_FEATURE_GROUP_NAME", featureSet.FeatureClassificationSystem.GroupName);
                }

                if (featureSet.Features != null)
                {
                    foreach (Feature feature in featureSet.Features)
                    {
                        Writer.WriteStartElement("FEATURE");
                        _writeOptionalElementString(Writer, "FNAME", feature.Name);
                        if (feature.Values != null)
                        {
                            foreach (string value in feature.Values)
                            {
                                _writeOptionalElementString(Writer, "FVALUE", value);
                            }
                        }
                        _writeOptionalElementString(Writer, "FUNIT", extensions, feature.Unit);
                        _writeOptionalElementString(Writer, "FDESCR", feature.Description);
                        _writeOptionalElementString(Writer, "FORDER", feature.Order);
                        Writer.WriteEndElement(); // !FEATURE
                    }
                }
                Writer.WriteEndElement(); // !PRODUCT_FEATURES
            }
        } // !_writeFeatureSets()


        private void _writeOptionalElementString(XmlTextWriter writer, string tagName, BMECatExtensions extensions, QuantityCode value)
        {
            if (value == null || (value.ClearText == null && (value.Code == null || value.Code == QuantityCodes.Unknown)))
            {
                return;
            }
            _writeOptionalElementString(writer, tagName, value, extensions);
        } // !_writeOptionalElementString() for QuantityCode with null check


        private void _writeLogisticsDetails(LogisticsDetails logistics)
        {
            if (logistics == null)
            {
                return;
            }

            Writer.WriteStartElement("PRODUCT_LOGISTIC_DETAILS");
            if (logistics.CustomsTariffNumber != null)
            {
                foreach (string tariff in logistics.CustomsTariffNumber)
                {
                    if (!string.IsNullOrEmpty(tariff))
                    {
                        Writer.WriteStartElement("CUSTOMS_TARIFF_NUMBER");
                        Writer.WriteElementString("CUSTOMS_NUMBER", tariff);
                        Writer.WriteEndElement(); // !CUSTOMS_TARIFF_NUMBER
                    }
                }
            }
            if (logistics.CountryOfOrigin.HasValue)
            {
                Writer.WriteElementString("COUNTRY_OF_ORIGIN", logistics.CountryOfOrigin.Value.EnumToString());
            }
            bool hasDimensions = logistics.Weight.HasValue || logistics.Length.HasValue ||
                                  logistics.Width.HasValue || logistics.Depth.HasValue || logistics.Volume.HasValue;
            if (hasDimensions)
            {
                Writer.WriteStartElement("PRODUCT_DIMENSIONS");
                if (logistics.Volume.HasValue)
                {
                    Writer.WriteElementString("VOLUME", _formatDecimal(logistics.Volume.Value));
                }
                if (logistics.Weight.HasValue)
                {
                    Writer.WriteElementString("WEIGHT", _formatDecimal(logistics.Weight.Value));
                }
                if (logistics.Length.HasValue)
                {
                    Writer.WriteElementString("LENGTH", _formatDecimal(logistics.Length.Value));
                }
                if (logistics.Width.HasValue)
                {
                    Writer.WriteElementString("WIDTH", _formatDecimal(logistics.Width.Value));
                }
                if (logistics.Depth.HasValue)
                {
                    Writer.WriteElementString("DEPTH", _formatDecimal(logistics.Depth.Value));
                }
                Writer.WriteEndElement(); // !PRODUCT_DIMENSIONS
            }
            Writer.WriteEndElement(); // !PRODUCT_LOGISTIC_DETAILS
        } // !_writeLogisticsDetails()


        private void _writeReferences(IList<Reference> references)
        {
            if (references == null || references.Count == 0)
            {
                return;
            }

            foreach (Reference reference in references)
            {
                string typeStr = _referenceTypeToString(reference.Type);
                if (typeStr == null)
                {
                    continue;
                }

                Writer.WriteStartElement("PRODUCT_REFERENCE");
                Writer.WriteAttributeString("type", typeStr);
                _writeOptionalElementString(Writer, "PROD_ID_TO", reference.IdTo);
                Writer.WriteEndElement(); // !PRODUCT_REFERENCE
            }
        } // !_writeReferences()


        private void _writeCatalogGroupMappings(IList<ProductCatalogGroupMapping> mappings)
        {
            if (mappings == null || mappings.Count == 0)
            {
                return;
            }

            foreach (ProductCatalogGroupMapping mapping in mappings)
            {
                Writer.WriteStartElement("PRODUCT_TO_CATALOGGROUP_MAP");
                _writeOptionalElementString(Writer, "CATALOG_GROUP_ID", mapping.CatalogGroupId);
                if (mapping.Order.HasValue)
                {
                    Writer.WriteElementString("PRODUCT_TO_CATALOGGROUP_MAP_ORDER", mapping.Order.Value.ToString());
                }
                Writer.WriteEndElement(); // !PRODUCT_TO_CATALOGGROUP_MAP
            }
        } // !_writeCatalogGroupMappings()


        private static string _referenceTypeToString(ReferenceTypes type)
        {
            switch (type)
            {
                case ReferenceTypes.SparePart: return "sparepart";
                case ReferenceTypes.Accessories: return "accessories";
                case ReferenceTypes.ConsistsOf: return "consists_of";
                case ReferenceTypes.Similar: return "similar";
                case ReferenceTypes.Select: return "select";
                case ReferenceTypes.Mandatory: return "mandatory";
                case ReferenceTypes.FollowUp: return "followup";
                case ReferenceTypes.BaseProduct: return "base_product";
                case ReferenceTypes.Others: return "others";
                default: return null;
            }
        } // !_referenceTypeToString()
    }
}
