import React, { Component } from "react";
import { connect } from "react-redux";
import { fetchRPs, deleteRP } from "./actions";
import { Link } from 'react-router-dom';
import {
    Table,
    Button
} from 'reactstrap';

export class RelyingParties extends Component {
    componentDidMount() {
        this.props.fetchRPs();
    }

    render() {
        return (
            <div>
                <h2>Relying Parties</h2>
                <Link to='/rps/new'><Button className="float-right">Create</Button></Link>
                <br/><br/>
                <Table striped>
                    <thead>
                        <tr>
                            <th scope="col">Realm</th>
                            <th scope="col">Name</th>
                            <th scope="col">Reply Url</th>
                            <th scope="col"></th>
                        </tr>
                    </thead>
                    <tbody>
                        {this.props.rps.map(el => (
                            <tr key={el.id}>
                                <th scope="row">{el.realm}</th>
                                <td>{el.name}</td>
                                <td>{el.replyUrl}</td>
                                <td>
                                <Link to={`/rps/${el.id}/edit`}><Button>Edit</Button></Link>{' '}<Button onClick={(e) => this.handleDelete(el.id, e)}>Delete</Button>
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </Table>
            </div>
        );
    }

    handleDelete = (id, e) => {
        e.preventDefault();
        this.props.deleteRP(id);
    }
}


function mapStateToProps(state) {
    return {
        rps: state.rps
    };
}

export default connect(
    mapStateToProps,
    { 
        fetchRPs,
        deleteRP
    }
)(RelyingParties);