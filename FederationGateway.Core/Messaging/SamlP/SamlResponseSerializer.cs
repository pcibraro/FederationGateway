using Microsoft.IdentityModel.Tokens.Saml2;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace FederationGateway.Core.Messaging.SamlP
{
    public class SamlResponseSerializer
    {
        const string SamlProtocolNamespace = "urn:oasis:names:tc:SAML:2.0:protocol";
        const string Saml2Namespace = "urn:oasis:names:tc:SAML:2.0:assertion";

        public void Serialize(XmlWriter writer, SamlResponseMessage response)
        {
            if(writer == null) throw new ArgumentNullException(nameof(writer));
            if(response == null) throw new ArgumentNullException(nameof(response));

            writer.WriteStartElement("samlp", response.ResponseType, SamlProtocolNamespace);
            writer.WriteAttributeString("IssueInstant", XmlConvert.ToString(DateTime.Now, XmlDateTimeSerializationMode.Utc));
            writer.WriteAttributeString("ID", "_" + response.Id);
            writer.WriteAttributeString("Version", "2.0");
            writer.WriteAttributeString("Destination", response.ReplyTo.ToString());
            if (!string.IsNullOrWhiteSpace(response.InResponseTo))
            {
                writer.WriteAttributeString("InResponseTo", response.InResponseTo);
            }
            writer.WriteStartElement("Issuer", Saml2Namespace);
            writer.WriteString(response.Issuer);
            writer.WriteEndElement();
            writer.WriteStartElement("samlp", "Status", SamlProtocolNamespace);
            writer.WriteStartElement("samlp", "StatusCode", SamlProtocolNamespace);
            writer.WriteAttributeString("Value", "urn:oasis:names:tc:SAML:2.0:status:Success");
            writer.WriteEndElement();
            writer.WriteEndElement();

            if (response.Token != null)
            {
                Saml2Serializer serializer = new Saml2Serializer();
                serializer.WriteAssertion(writer, response.Token.Assertion);
            }

            writer.WriteEndElement();
        }
    }
}
