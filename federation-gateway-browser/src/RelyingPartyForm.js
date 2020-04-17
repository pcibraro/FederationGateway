import React from 'react';
import {
  Form,
  FormGroup,
  Input,
  FormFeedback,
  Button,
  Label
} from 'reactstrap';

class RelyingPartyForm extends React.Component {
  state = {
    id: this.props.rp ? this.props.rp.id : null,
    name: this.props.rp ? this.props.rp.name : '',
    realm: this.props.rp ? this.props.rp.realm : '',
    replyUrl: this.props.rp ? this.props.rp.replyUrl : '',
    logoutUrl: this.props.rp ? this.props.rp.logoutUrl : '',
    tokenLifetime: this.props.rp ? this.props.rp.tokenLifetime : 60,
    errors: {},
    loading: false
  }

  /*componentWillReceiveProps(nextProps) {
    if (nextProps.rp) {
      this.setState({
        id: nextProps.rp.id,
        realm: nextProps.rp.realm,
        replyUrl: nextProps.rp.replyUrl,
        logoutUrl: nextProps.rp.logoutUrl,
        tokenLifetime: nextProps.rp.tokenLifetime
      });
    }
  }*/
  

  handleChange = (e) => {
    if (!!this.state.errors[e.target.name]) {
      let errors = Object.assign({}, this.state.errors);
      delete errors[e.target.name]
      this.setState({
        [e.target.name]: e.target.value,
        errors
      });
    } else {
      this.setState({ [e.target.name]: e.target.value });
    }
  }

  handleSubmit = (e) => {
    e.preventDefault();

    let errors = {}
    if (this.state.realm === '') errors.realm = "Realm is required";
    if (this.state.replyUrl === '') errors.replyUrl = "Reply Url is required";
    if (this.state.name === '') errors.name = "Name is required";
    
    this.setState({ errors });
    const isValid = Object.keys(errors).length === 0;

    if (isValid) {
      const { id, realm, name, replyUrl, logoutUrl, tokenLifetime } = this.state;
      console.log("name " + name);
      this.setState({ loading: true });
      this.props.saveRP({ id, realm, name, replyUrl, logoutUrl, tokenLifetime })
        .catch((err) => err.response.json().then(({ errors }) => this.setState({ errors, loading: false })));
    }
  }

  handleCancel = (e) => {
    e.preventDefault();
    this.props.cancelSaveRP();
  }

  render() {


    return (
      <Form onSubmit={this.handleSubmit}>

        <div>
          {this.props.rp ? (
            <h1>Edit Relying Party</h1>
          ) : (
              <h1>Add New Relying Party</h1>
            )}
        </div>

        <FormGroup>
          <Label for="realm">Realm</Label>
          <Input name="realm" id="realm" value={this.state.realm} onChange={this.handleChange} invalid={!!this.state.errors.realm} />
          {!!this.state.errors.realm && <FormFeedback>{this.state.errors.realm}</FormFeedback>}
        </FormGroup>
        <FormGroup>
          <Label for="name">Name</Label>
          <Input name="name" id="name" value={this.state.name} onChange={this.handleChange} invalid={!!this.state.errors.name} />
          {!!this.state.errors.name && <FormFeedback>{this.state.errors.name}</FormFeedback>}
        </FormGroup>
        <FormGroup>
          <Label for="replyUrl">Reply Url</Label>
          <Input name="replyUrl" id="replyUrl" type="url" value={this.state.replyUrl} onChange={this.handleChange} invalid={!!this.state.errors.replyUrl} />
          {!!this.state.errors.replyUrl && <FormFeedback>{this.state.errors.replyUrl}</FormFeedback>}
        </FormGroup>
        <FormGroup>
          <Label for="logoutUrl">Logout Url</Label>
          <Input name="logoutUrl" id="logoutUrl" type="url" value={this.state.logoutUrl} onChange={this.handleChange} invalid={!!this.state.errors.logoutUrl} />
          {!!this.state.errors.logoutUrl && <FormFeedback>{this.state.errors.logoutUrl}</FormFeedback>}
        </FormGroup>
        <FormGroup>
          <Label for="tokenLifetime">Token Lifetime</Label>
          <Input name="tokenLifetime" id="tokenLifetime" type="number" value={this.state.tokenLifetime} onChange={this.handleChange} invalid={!!this.state.errors.tokenLifetime} />
          {!!this.state.errors.tokenLifetime && <FormFeedback>{this.state.errors.tokenLifetime}</FormFeedback>}
        </FormGroup>
        <Button>Save</Button> {' '} <Button onClick={this.handleCancel}>Cancel</Button>
      </Form>
    );
  }
}

export default RelyingPartyForm;
