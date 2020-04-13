using FederationGateway.Core.Keys;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;

namespace FederationGateway.Core.Messaging.Metadata
{
    public class WsFederationMetadataSerializer
    {
        const string Saml20Namespace = "urn:oasis:names:tc:SAML:2.0:metadata";
        const string WsFed200706Namespace = "http://docs.oasis-open.org/wsfed/federation/200706";
        const string WsAddressing200508Namespace = "http://www.w3.org/2005/08/addressing";
        const string XmlDSigNamespace = "http://www.w3.org/2000/09/xmldsig#";
        public WsFederationMetadataSerializer()
        {
        }

        public void Serialize(XmlWriter writer, 
            X509Certificate2 signingKey, 
            string id, 
            string issuerName, 
            string samlUrl, 
            string wsFedUrl)
        {

            if (writer == null) throw new ArgumentNullException(nameof(writer));
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            if (string.IsNullOrWhiteSpace(issuerName)) throw new ArgumentNullException(nameof(issuerName));
            if (string.IsNullOrWhiteSpace(samlUrl)) throw new ArgumentNullException(nameof(samlUrl));
            if (string.IsNullOrWhiteSpace(wsFedUrl)) throw new ArgumentNullException(nameof(wsFedUrl));

            var keyInfo = new KeyInfoX509Data(signingKey);

            keyInfo.AddIssuerSerial(signingKey.IssuerName.Name, signingKey.SerialNumber);
            keyInfo.AddSubjectName(signingKey.SubjectName.Name);

            var keyInfoXml = keyInfo.GetXml().OuterXml;

            writer.WriteStartElement("EntityDescriptor", Saml20Namespace);
            writer.WriteAttributeString("ID", id);
            writer.WriteAttributeString("entityID", issuerName);

            //IDPSSODescriptor
            writer.WriteStartElement("IDPSSODescriptor", Saml20Namespace);
            writer.WriteAttributeString("protocolSupportEnumeration", "urn:oasis:names:tc:SAML:2.0:protocol");

            //KeyDescriptor
            writer.WriteStartElement("KeyDescriptor", Saml20Namespace);
            writer.WriteAttributeString("use", "signing");
            writer.WriteStartElement("KeyInfo", XmlDSigNamespace);
            writer.WriteRaw(keyInfoXml);
            writer.WriteEndElement();
            writer.WriteEndElement();

            //SingleLogoutService
            writer.WriteStartElement("SingleLogoutService", Saml20Namespace);
            writer.WriteAttributeString("Binding", "urn:oasis:names:tc:SAML:2.0:bindings:HTTP-POST");
            writer.WriteAttributeString("Location", samlUrl);
            writer.WriteEndElement();

            //SingleSignOnService
            writer.WriteStartElement("SingleSignOnService", Saml20Namespace);
            writer.WriteAttributeString("Binding", "urn:oasis:names:tc:SAML:2.0:bindings:HTTP-POST");
            writer.WriteAttributeString("Location", samlUrl);
            writer.WriteEndElement();

            writer.WriteEndElement();

            //RoleDescriptor
            WriteRoleDescriptor(writer, issuerName, wsFedUrl, keyInfoXml, "SecurityTokenServiceType");

            //RoleDescriptor
            WriteRoleDescriptor(writer, issuerName, wsFedUrl, keyInfoXml, "ApplicationServiceType");

            writer.WriteEndElement();
        }

        private static void WriteRoleDescriptor(XmlWriter writer, 
            string issuerName, 
            string wsFedUrl, 
            string keyInfoXml,
            string roleType)
        {
            writer.WriteStartElement("RoleDescriptor", Saml20Namespace);
            writer.WriteAttributeString("protocolSupportEnumeration", "http://docs.oasis-open.org/ws-sx/ws-trust/200512 http://schemas.xmlsoap.org/ws/2005/02/trust http://docs.oasis-open.org/wsfed/federation/200706");
            writer.WriteAttributeString("ServiceDisplayName", issuerName);
            writer.WriteAttributeString("xmlns", "fed", null, WsFed200706Namespace);
            writer.WriteAttributeString("type", "http://www.w3.org/2001/XMLSchema-instance", "fed:" + roleType);

            writer.WriteStartElement("KeyDescriptor", Saml20Namespace);
            writer.WriteAttributeString("use", "signing");
            writer.WriteStartElement("KeyInfo", XmlDSigNamespace);
            writer.WriteRaw(keyInfoXml);
            writer.WriteEndElement();
            writer.WriteEndElement();

            writer.WriteStartElement("TokenTypeOffered", WsFed200706Namespace);
            writer.WriteStartElement("TokenType");
            writer.WriteAttributeString("Uri", "urn:oasis:names:tc:SAML:2.0:assertion");
            writer.WriteEndElement();
            writer.WriteEndElement();

            writer.WriteStartElement("PassiveRequestorEndpoint", WsFed200706Namespace);
            writer.WriteStartElement("EndpointReference", WsAddressing200508Namespace);
            writer.WriteElementString("Address", WsAddressing200508Namespace, wsFedUrl);
            writer.WriteEndElement();
            writer.WriteEndElement();
            
            writer.WriteEndElement();
        }



    }
}
