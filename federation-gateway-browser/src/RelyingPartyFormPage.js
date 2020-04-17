import React from 'react';
import { connect } from 'react-redux';
import { Redirect } from 'react-router';
import { saveRP, fetchRP, updateRP } from './actions';
import RelyingPartyForm from './RelyingPartyForm';

class RelyingPartyFormPage extends React.Component {
  state = {
    redirect: false,
  }

  componentDidMount() {
    const { match } = this.props;
    
    if (match.params.id) {
      this.props.fetchRP(match.params.id);
    }
  }

  saveRP = ({ id, realm, name, replyUrl, logoutUrl, tokenLifetime }) => {
    if (id) {
      return this.props.updateRP({ id, realm, name, replyUrl, logoutUrl, tokenLifetime })
        .then(
          () => { this.setState({ redirect: true })},
        );
    } else {
      return this.props.saveRP({ realm, name, replyUrl, logoutUrl, tokenLifetime })
        .then(
          () => { this.setState({ redirect: true })},
        );
    }
  }

  cancelSaveRP = () => {
    this.setState({ redirect: true });
  }

  render() {
    return (
      <div>
        { this.state.redirect ? (
          <Redirect to="/rps" />
        ) : (
          <RelyingPartyForm
            rp={this.props.rp}
            saveRP={this.saveRP}
            cancelSaveRP={this.cancelSaveRP}
          />
        )}
      </div>
    );
  }
}

function mapStateToProps(state, props) {
  const { match } = props;

  if (match.params.id) {
    return {
      rp: state.rps.find(item => item.id === match.params.id)
    }
  }

  return { rp: null };
}

export default connect(mapStateToProps, { saveRP, fetchRP, updateRP })(RelyingPartyFormPage);
