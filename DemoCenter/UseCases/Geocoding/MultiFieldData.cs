//--------------------------------------------------------------
// Copyright (c) PTV Group
// 
// For license details, please refer to the file COPYING, which 
// should have been provided with this distribution.
//--------------------------------------------------------------

namespace Ptv.XServer.Demo.Geocoding
{
    /// <summary> Address data which can be entered for a multi field geocoding. </summary>
    public class MultiFieldData
    {
        /// <summary> Gets or sets the country of the search address. </summary>
        public string Country { get; set; }

        /// <summary> Gets or sets the state of the search address. </summary>
        public string State { get; set; }

        /// <summary> Gets or sets the postal code of the search address. </summary>
        public string PostalCode { get; set; }

        /// <summary> Gets or sets the city of the search address. </summary>
        public string City { get; set; }

        /// <summary> Gets or sets the street of the search address. </summary>
        public string Street { get; set; }

        /// <summary> Checks whether the search address is empty. </summary>
        /// <returns> A value indicating whether the search address is empty. </returns>
        public bool IsEmpty()
        {
            bool result = true;
            result &= string.IsNullOrEmpty(Country);
            result &= string.IsNullOrEmpty(State);
            result &= string.IsNullOrEmpty(PostalCode);
            result &= string.IsNullOrEmpty(City);
            result &= string.IsNullOrEmpty(Street);
            
            return result;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return string.Format("{0} / {1} / {2} / {3} / {4}", Country, State, PostalCode, City, Street);
        }
    }
}
