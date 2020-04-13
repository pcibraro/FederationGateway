using Microsoft.IdentityModel.Tokens.Saml2;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace FederationGateway.Core.Messaging.WsTrust
{
    public class WsTrustSerializer
    {
        const string WsTrust200502Namespace = "http://schemas.xmlsoap.org/ws/2005/02/trust";
        const string WsSecurityUtility10Namespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";
        const string WsPolicy200409Namespace = "http://schemas.xmlsoap.org/ws/2004/09/policy";
        const string WsAddressing200508Namespace = "http://www.w3.org/2005/08/addressing";

        public WsTrustSerializer()
        {
        }

        public void Serialize(XmlWriter writer, WsTrustRequestSecurityTokenResponse response)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            if (response == null) throw new ArgumentNullException(nameof(response));

            writer.WriteStartElement("t", "RequestSecurityTokenResponse", WsTrust200502Namespace);

            WriteLifetime(writer, response.LifeTime);

            WriteAppliesTo(writer, response.AppliesTo);

            WriteRequestedSecurityToken(writer, response.RequestedSecurityToken);

            writer.WriteElementString("t", "TokenType", WsTrust200502Namespace, "urn:oasis:names:tc:SAML:1.0:assertion");
            writer.WriteElementString("t", "RequestType", WsTrust200502Namespace, "http://schemas.xmlsoap.org/ws/2005/02/trust/Issue");
            writer.WriteElementString("t", "KeyType", WsTrust200502Namespace, "http://schemas.xmlsoap.org/ws/2005/05/identity/NoProofKey");

            writer.WriteEndElement();
        }

        private void WriteLifetime(XmlWriter writer, WsTrustLifetime lifetime)
        {
            writer.WriteStartElement("t", "Lifetime", WsTrust200502Namespace);
            writer.WriteElementString("wsu", "Created", WsSecurityUtility10Namespace,
                XmlConvert.ToString(lifetime.Created, XmlDateTimeSerializationMode.Utc));
            writer.WriteElementString("wsu", "Expires", WsSecurityUtility10Namespace,
                XmlConvert.ToString(lifetime.Expires, XmlDateTimeSerializationMode.Utc));
            writer.WriteEndElement();
        }

        private void WriteAppliesTo(XmlWriter writer, Uri address)
        {
            writer.WriteStartElement("wsp", "AppliesTo", WsPolicy200409Namespace);
            writer.WriteStartElement("wsa", "EndpointReference", WsAddressing200508Namespace);
            writer.WriteElementString("wsa", "Address", WsAddressing200508Namespace, address.ToString());
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        private void WriteRequestedSecurityToken(XmlWriter writer, Saml2SecurityToken token)
        {
            writer.WriteStartElement("t", "RequestedSecurityToken", WsTrust200502Namespace);
            
            Saml2Serializer serializer = new Saml2Serializer();
            serializer.WriteAssertion(writer, token.Assertion);

            writer.WriteEndElement();
        }
    }
}
