﻿    /**
    * @ngdoc model
    * @name merchello.models.shipment
    * @function
    * 
    * @description
    * Represents a JS version of Merchello's ShipmentDetails object
    */
    Merchello.Models.Shipment = function () {

        var self = this;

        self.key = '';
        self.shipmentNumber = '';
        self.shipmentNumberPrefix = '';
        self.versionKey = '';
        self.fromOrganization = '';
        self.fromName = '';
        self.fromAddress1 = '';
        self.fromAddress2 = '';
        self.fromLocality = '';
        self.fromRegion = '';
        self.fromPostalCode = '';
        self.fromCountryCode = '';
        self.fromIsCommercial = '';
        self.toOrganization = '';
        self.toName = '';
        self.toAddress1 = '';
        self.toAddress2 = '';
        self.toLocality = '';
        self.toRegion = '';
        self.toPostalCode = '';
        self.toCountryCode = '';
        self.toIsCommercial = '';
        self.shipMethodKey = '';
        self.phone = '';
        self.email = '';
        self.carrier = '';
        self.trackingCode = '';
        self.shippedDate = '';
        self.items = [];
        self.shipmentStatus = new Merchello.Models.ShipmentStatus();
    };

    // Shipment Prototype
    // ------------------------------------------------
    Merchello.Models.Shipment.prototype = (function () {

        //// Private members

            // returns the shipment destination as an Address
        var getDestinationAddress = function() {
                return buildAddress(this.toName, this.toAddress1, this.toAddress2, this.toLocality, this.toRegion, this.toPostalCode, this.toCountryCode, this.toOrganization, this.toIsCommercial, '', '', 'shipping');
            },

            // returns the shipment origin as an Address
            getOriginAddress = function() {
                return buildAddress(this.fromName, this.fromAddress1, this.fromAddress2, this.fromLocality, this.fromRegion, this.fromPostalCode, this.fromCountryCode, this.fromOrganization, this.fromIsCommercial, '', '', 'shipping');
            },

            // Utility to build an address
            buildAddress = function(name, address1, address2, locality, region, postalCode, countryCode, organization, isCommercial, phone, email, addressType) {
                var adr = new Merchello.Models.Address();
                adr.name = name;
                adr.address1 = address1;
                adr.address2 = address2;
                adr.locality = locality;
                adr.region = region;
                adr.postalCode = postalCode;
                adr.countryCode = countryCode;
                adr.organization = organization;
                adr.isCommercial = isCommercial;
                adr.phone = phone;
                adr.email = email;
                adr.addressType = addressType;
                return adr;
            };

        // public members
        return {
            /**
            * @ngdoc prototype
            * @name merchello.models.Shipment.getDestinationAddress
            * @function
            * 
            * @description
            * Returns a merchello.models.Address representing the shipment destination
            */
            getDestinationAddress: getDestinationAddress,

            /**
            * @ngdoc prototype
            * @name merchello.models.Shipment.getOriginAddress
            * @function
            * 
            * @description
            * Returns a merchello.models.Address representing the shipment origin
            */
            getOriginAddress: getOriginAddress
        };

    }());
